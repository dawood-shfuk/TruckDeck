using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;

namespace Funbit.Ets.Telemetry.Server.Helpers
{
    public static class NetworkHelper
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static NetworkInterfaceInfo[] GetAllActiveNetworkInterfaces()
        {
            return GetScoredCandidates()
                .Select(c => c.Info)
                .Distinct()
                .ToArray();
        }

        public static NetworkInterfaceInfo GetPreferredNetworkInterface()
        {
            var best = GetScoredCandidates()
                .OrderByDescending(c => c.Score)
                .FirstOrDefault();

            if (best == null)
                throw new Exception(
                    "System does not have any registered network interfaces that are connected to a network.");

            return best.Info;
        }

        static ScoredInterface[] GetScoredCandidates()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            Log.InfoFormat("Found following network interfaces: {0}{1}", Environment.NewLine,
                string.Join(", " + Environment.NewLine,
                    interfaces.Select(a => $"'{a.Id}': '{a.Name}' ({a.OperationalStatus})")));

            return interfaces
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses
                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(ua => new ScoredInterface
                    {
                        Info = new NetworkInterfaceInfo
                        {
                            Id = n.Id,
                            Name = n.Name,
                            Ip = ua.Address.ToString(),
                            DhcpEnabled = IsDhcpEnabled(n),
                            IsPrivate = IsPrivateLan(ua.Address)
                        },
                        Score = ScoreInterface(n, ua)
                    }))
                .Where(c => c.Score > -500)
                .ToArray();
        }

        static bool IsDhcpEnabled(NetworkInterface nic)
        {
            try
            {
                var ipv4 = nic.GetIPProperties().GetIPv4Properties();
                return ipv4 != null && ipv4.IsDhcpEnabled;
            }
            catch
            {
                return false;
            }
        }

        static bool IsPrivateLan(IPAddress address)
        {
            if (IPAddress.IsLoopback(address))
                return false;

            var bytes = address.GetAddressBytes();
            if (bytes.Length != 4)
                return false;

            if (bytes[0] == 10)
                return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            return false;
        }

        static int ScoreInterface(NetworkInterface nic, UnicastIPAddressInformation ua)
        {
            var ip = ua.Address;
            var ipText = ip.ToString();

            if (IPAddress.IsLoopback(ip))
                return -1000;

            if (ipText.StartsWith("169.254.", StringComparison.Ordinal))
                return -800;

            var score = 0;

            if (IsPrivateLan(ip))
                score += 120;

            if (IsDhcpEnabled(nic))
                score += 60;

            switch (nic.NetworkInterfaceType)
            {
                case NetworkInterfaceType.Ethernet:
                    score += 40;
                    break;
                case NetworkInterfaceType.Wireless80211:
                    score += 35;
                    break;
                case NetworkInterfaceType.GigabitEthernet:
                    score += 38;
                    break;
            }

            var name = (nic.Name + " " + nic.Description).ToLowerInvariant();
            if (name.Contains("virtual") || name.Contains("vmware") || name.Contains("hyper-v")
                || name.Contains("vethernet") || name.Contains("wsl") || name.Contains("docker")
                || name.Contains("tap") || name.Contains("tunnel") || name.Contains("loopback")
                || name.Contains("bluetooth"))
            {
                score -= 90;
            }

            if (name.Contains("ethernet") || name.Contains("wi-fi") || name.Contains("wifi"))
                score += 10;

            return score;
        }

        sealed class ScoredInterface
        {
            public NetworkInterfaceInfo Info { get; set; }
            public int Score { get; set; }
        }
    }

    public class NetworkInterfaceInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Ip { get; set; }
        public bool DhcpEnabled { get; set; }
        public bool IsPrivate { get; set; }

        public string AdapterSummary
        {
            get
            {
                var dhcp = DhcpEnabled ? "DHCP" : "static";
                return $"{Name} · {dhcp}";
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
