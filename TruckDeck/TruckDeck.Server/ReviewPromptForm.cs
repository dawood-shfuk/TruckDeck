using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;
using Funbit.Ets.Telemetry.Server.Services;
using Newtonsoft.Json.Linq;

namespace Funbit.Ets.Telemetry.Server
{
    public class ReviewPromptForm : Form
    {
        public string ReviewUrl { get; private set; }

        public ReviewPromptForm()
        {
            Text = "Rate TruckDeck";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(400, 140);

            var label = new Label
            {
                AutoSize = false,
                Size = new Size(360, 50),
                Location = new Point(20, 20),
                Text = "Request a secure review link from truckdeck.site?"
            };

            var ok = new Button { Text = "Continue", Location = new Point(20, 80), Size = new Size(100, 32) };
            ok.Click += async (_, __) => await RequestLinkAsync(ok);

            var cancel = new Button { Text = "Cancel", Location = new Point(130, 80), Size = new Size(100, 32) };
            cancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(label);
            Controls.Add(ok);
            Controls.Add(cancel);
        }

        async Task RequestLinkAsync(Button ok)
        {
            ok.Enabled = false;
            try
            {
                var res = await TruckDeckApiClient.PostSignedAsync("/api/v1/reviews/eligibility", new
                {
                    total_runtime_seconds = ClientState.Instance.TotalRuntimeSeconds,
                    telemetry_connected_sessions = ClientState.Instance.TelemetryConnectedSessions,
                    app_version = AssemblyHelper.Version,
                });
                if (!res.IsSuccessStatusCode)
                {
                    MessageBox.Show(this, "Not eligible yet — use TruckDeck with the game connected for 30+ minutes.",
                        "Rate TruckDeck", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ok.Enabled = true;
                    return;
                }

                var doc = JObject.Parse(await res.Content.ReadAsStringAsync());
                if (!doc.Value<bool>("eligible"))
                {
                    MessageBox.Show(this, "Not eligible yet — keep trucking and try again later.",
                        "Rate TruckDeck", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ok.Enabled = true;
                    return;
                }

                ReviewUrl = doc.Value<string>("review_url");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch
            {
                MessageBox.Show(this, "Could not reach truckdeck.site.", "Rate TruckDeck",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ok.Enabled = true;
            }
        }
    }
}
