using System;
using System.Configuration;
using System.Linq;
using Funbit.Ets.Telemetry.Server.Helpers;

namespace Funbit.Ets.Telemetry.Server.Setup
{
    /// <summary>Shared HTTP URL ACL + firewall port list for telemetry (25555) and input bridge (25556).</summary>
    public static class HttpPortSetupHelper
    {
        public static int TelemetryPort
        {
            get
            {
                int p;
                return int.TryParse(ConfigurationManager.AppSettings["Port"], out p) && p > 0 ? p : 25555;
            }
        }

        public static int InputBridgePort
        {
            get
            {
                int p;
                return int.TryParse(ConfigurationManager.AppSettings["InputBridgePort"], out p) && p > 0 ? p : 25556;
            }
        }

        public static int[] AllPorts => new[] { TelemetryPort, InputBridgePort };

        public static string PortListLabel => string.Join(", ", AllPorts.Distinct());

        public static bool HasUrlReservation(int port)
        {
            try
            {
                var output = ProcessHelper.RunNetShell($@"http show urlacl url=http://+:{port}/", "Failed to check URL ACL");
                return output.Contains($":{port}/");
            }
            catch
            {
                return false;
            }
        }

        public static bool HasAllUrlReservations() => AllPorts.Distinct().All(HasUrlReservation);

        public static void EnsureUrlReservation(int port)
        {
            if (HasUrlReservation(port))
                return;

            var everyone = new System.Security.Principal.SecurityIdentifier("S-1-1-0")
                .Translate(typeof(System.Security.Principal.NTAccount)).ToString();
            var arguments = string.Format("http add urlacl url=http://+:{0}/ user=\"\\{1}\"", port, everyone);
            ProcessHelper.RunNetShell(arguments, $"Failed to add URL ACL for port {port}");
        }

        public static void EnsureAllUrlReservations()
        {
            foreach (var port in AllPorts.Distinct())
                EnsureUrlReservation(port);
        }

        public static void RemoveUrlReservation(int port)
        {
            if (!HasUrlReservation(port))
                return;
            ProcessHelper.RunNetShell($@"http delete urlacl url=http://+:{port}/", $"Failed to delete URL ACL for port {port}");
        }

        public static void RemoveAllUrlReservations()
        {
            foreach (var port in AllPorts.Distinct())
                RemoveUrlReservation(port);
        }

        public static string FirewallRuleName(int port) => $"TRUCKDECK (PORT {port})";

        public static bool HasFirewallRule(int port)
        {
            try
            {
                var output = ProcessHelper.RunNetShell("advfirewall firewall show rule dir=in name=all", "Failed to check Firewall");
                var ruleName = FirewallRuleName(port);
                return output.Contains(ruleName) && output.Contains(port.ToString());
            }
            catch
            {
                return false;
            }
        }

        public static bool HasAllFirewallRules() => AllPorts.Distinct().All(HasFirewallRule);

        public static void EnsureFirewallRule(int port)
        {
            if (HasFirewallRule(port))
                return;

            var arguments = $"advfirewall firewall add rule name=\"{FirewallRuleName(port)}\" " +
                            $"dir=in action=allow protocol=TCP localport={port} remoteip=localsubnet";
            ProcessHelper.RunNetShell(arguments, $"Failed to add Firewall rule for port {port}");
        }

        public static void EnsureAllFirewallRules()
        {
            foreach (var port in AllPorts.Distinct())
                EnsureFirewallRule(port);
        }

        public static void RemoveFirewallRule(int port)
        {
            try
            {
                ProcessHelper.RunNetShell($"advfirewall firewall delete rule name=\"{FirewallRuleName(port)}\"",
                    "Failed to delete Firewall rule");
            }
            catch
            {
                /* rule may not exist */
            }
        }

        public static void RemoveAllFirewallRules()
        {
            foreach (var port in AllPorts.Distinct())
                RemoveFirewallRule(port);
        }
    }
}
