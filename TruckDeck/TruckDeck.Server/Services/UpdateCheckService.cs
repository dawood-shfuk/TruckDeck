using System;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;
using Newtonsoft.Json.Linq;

namespace Funbit.Ets.Telemetry.Server.Services
{
    public static class UpdateCheckService
    {
        static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        public static async Task CheckForUpdateAsync(Control owner)
        {
            var state = ClientState.Instance;
            var intervalHours = 24;
            int.TryParse(ConfigurationManager.AppSettings["UpdateCheckIntervalHours"], out intervalHours);
            if (intervalHours < 1) intervalHours = 24;

            if (state.LastUpdateCheckUtc.HasValue
                && state.LastUpdateCheckUtc.Value.AddHours(intervalHours) > DateTime.UtcNow)
                return;

            state.LastUpdateCheckUtc = DateTime.UtcNow;
            state.Save();

            var url = ConfigurationManager.AppSettings["UpdateCheckUrl"]
                ?? "https://truckdeck.site/api/version";

            try
            {
                var json = await Http.GetStringAsync(url);
                var doc = JObject.Parse(json);
                var remote = doc.Value<string>("version");
                var downloadUrl = doc.Value<string>("download_url")
                    ?? "https://truckdeck.site/downloads/TruckDeck-Setup.exe";
                var local = AssemblyHelper.Version;

                if (string.IsNullOrWhiteSpace(remote) || !VersionComparer.IsNewer(remote, local))
                    return;

                if (string.Equals(state.SkippedUpdateVersion, remote, StringComparison.OrdinalIgnoreCase))
                    return;

                if (owner == null)
                    return;

                owner.BeginInvoke(new Action(() =>
                {
                    using (var form = new UpdateAvailableForm(remote, downloadUrl))
                        form.ShowDialog(owner);
                }));
            }
            catch
            {
                // offline / unreachable — silent
            }
        }
    }
}
