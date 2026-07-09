using System;
using System.IO;
using System.Text;
using Funbit.Ets.Telemetry.Server.Helpers;
using Newtonsoft.Json;

namespace Funbit.Ets.Telemetry.Server.Services
{
    public class ClientState
    {
        public string InstallId { get; set; }
        public string InstallKey { get; set; }
        public string SkippedUpdateVersion { get; set; }
        public DateTime? LastUpdateCheckUtc { get; set; }
        public bool CrashReportingEnabled { get; set; }

        static readonly string StateFile = Path.Combine(Settings.SettingsDirectory, "ClientState.json");
        static readonly Lazy<ClientState> LazyInstance = new Lazy<ClientState>(Load);

        public static ClientState Instance => LazyInstance.Value;

        public static ClientState Load()
        {
            if (!File.Exists(StateFile))
                return new ClientState();
            try
            {
                return JsonConvert.DeserializeObject<ClientState>(
                    File.ReadAllText(StateFile, Encoding.UTF8)) ?? new ClientState();
            }
            catch
            {
                return new ClientState();
            }
        }

        public void Save()
        {
            if (!Directory.Exists(Settings.SettingsDirectory))
                Directory.CreateDirectory(Settings.SettingsDirectory);
            File.WriteAllText(StateFile, JsonConvert.SerializeObject(this, Formatting.Indented), Encoding.UTF8);
        }
    }
}
