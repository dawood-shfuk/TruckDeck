using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;
using Newtonsoft.Json.Linq;

namespace Funbit.Ets.Telemetry.Server.Services
{
    public static class ReviewService
    {
        public const int MinRuntimeSeconds = 1800;

        public static void TickRuntime(bool serverRunning, bool telemetryConnected)
        {
            var state = ClientState.Instance;
            if (serverRunning)
                state.TotalRuntimeSeconds++;

            if (telemetryConnected && !state.TelemetryWasConnected)
            {
                state.TelemetryConnectedSessions++;
                state.TelemetryWasConnected = true;
            }
            else if (!telemetryConnected)
            {
                state.TelemetryWasConnected = false;
            }

            state.Save();
        }

        public static bool CanPrompt()
        {
            var state = ClientState.Instance;
            if (state.HasSubmittedReview) return false;
            if (state.ReviewPromptDismissedUntil.HasValue
                && state.ReviewPromptDismissedUntil.Value > DateTime.UtcNow)
                return false;
            return state.TotalRuntimeSeconds >= MinRuntimeSeconds
                && state.TelemetryConnectedSessions >= 1;
        }

        public static async Task TryOpenReviewFlowAsync(Control owner)
        {
            if (!CanPrompt()) return;

            try
            {
                var res = await TruckDeckApiClient.PostSignedAsync("/api/v1/reviews/eligibility", new
                {
                    total_runtime_seconds = ClientState.Instance.TotalRuntimeSeconds,
                    telemetry_connected_sessions = ClientState.Instance.TelemetryConnectedSessions,
                    app_version = AssemblyHelper.Version,
                });

                if (!res.IsSuccessStatusCode) return;

                var json = await res.Content.ReadAsStringAsync();
                var doc = JObject.Parse(json);
                if (!doc.Value<bool>("eligible")) return;

                var reviewUrl = doc.Value<string>("review_url");
                if (string.IsNullOrWhiteSpace(reviewUrl)) return;

                owner.BeginInvoke(new Action(() =>
                {
                    var result = MessageBox.Show(owner,
                        "You've been running TruckDeck for a while — would you like to rate it on truckdeck.site?",
                        "Rate TruckDeck",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);
                    if (result == DialogResult.Yes)
                    {
                        ProcessHelper.OpenUrl(reviewUrl);
                        ClientState.Instance.HasSubmittedReview = true;
                        ClientState.Instance.Save();
                    }
                    else
                    {
                        ClientState.Instance.ReviewPromptDismissedUntil = DateTime.UtcNow.AddDays(90);
                        ClientState.Instance.Save();
                    }
                }));
            }
            catch
            {
                // silent
            }
        }

        public static void OpenReviewPrompt(IWin32Window owner)
        {
            using (var form = new ReviewPromptForm())
            {
                if (form.ShowDialog(owner) == DialogResult.OK && !string.IsNullOrWhiteSpace(form.ReviewUrl))
                    ProcessHelper.OpenUrl(form.ReviewUrl);
            }
        }
    }
}
