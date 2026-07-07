using System;
using System.Drawing;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Services;

namespace Funbit.Ets.Telemetry.Server
{
    public class UpdateAvailableForm : Form
    {
        readonly string _remoteVersion;
        readonly string _downloadUrl;

        public UpdateAvailableForm(string remoteVersion, string downloadUrl)
        {
            _remoteVersion = remoteVersion;
            _downloadUrl = downloadUrl;

            Text = "TruckDeck update available";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 180);

            var label = new Label
            {
                AutoSize = false,
                Size = new Size(380, 60),
                Location = new Point(20, 20),
                Text = $"TruckDeck {_remoteVersion} is available on truckdeck.site.\r\nDownload the installer to update."
            };

            var download = new Button { Text = "Download", Location = new Point(20, 100), Size = new Size(100, 32) };
            download.Click += (_, __) =>
            {
                Helpers.ProcessHelper.OpenUrl(_downloadUrl);
                DialogResult = DialogResult.OK;
                Close();
            };

            var later = new Button { Text = "Later", Location = new Point(130, 100), Size = new Size(100, 32) };
            later.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            var skip = new Button { Text = "Skip this version", Location = new Point(240, 100), Size = new Size(120, 32) };
            skip.Click += (_, __) =>
            {
                ClientState.Instance.SkippedUpdateVersion = _remoteVersion;
                ClientState.Instance.Save();
                DialogResult = DialogResult.Ignore;
                Close();
            };

            Controls.Add(label);
            Controls.Add(download);
            Controls.Add(later);
            Controls.Add(skip);
        }
    }
}
