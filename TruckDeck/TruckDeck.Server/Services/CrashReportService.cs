using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;

namespace Funbit.Ets.Telemetry.Server.Services
{
    public static class CrashReportService
    {
        const int MaxLogTailLines = 80;
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void ReportAsync(Exception ex)
        {
            if (!ClientState.Instance.CrashReportingEnabled)
                return;

            _ = SendPayloadAsync(
                ex.GetType().FullName,
                ex.Message,
                ex.ToString());
        }

        public static async Task<bool> SendUserReportAsync(IWin32Window owner, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return false;

            if (!ClientState.Instance.CrashReportingEnabled)
            {
                var consent = MessageBox.Show(owner,
                    "Send an anonymous bug report to truckdeck.site? (app version, OS, and recent log lines — no personal data)",
                    "Report a bug",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (consent != DialogResult.Yes)
                    return false;

                ClientState.Instance.CrashReportingEnabled = true;
                ClientState.Instance.Save();
            }

            return await SendPayloadAsync(
                "UserReport",
                userMessage.Trim(),
                userMessage.Trim()).ConfigureAwait(true);
        }

        public static void PromptUserReport(IWin32Window owner)
        {
            using (var form = new Form())
            {
                form.Text = "Report a bug";
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.ClientSize = new System.Drawing.Size(420, 200);
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var label = new Label
                {
                    Text = "What went wrong? (brief description)",
                    AutoSize = true,
                    Location = new System.Drawing.Point(12, 12)
                };
                var box = new TextBox
                {
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    Location = new System.Drawing.Point(12, 36),
                    Size = new System.Drawing.Size(396, 96)
                };
                var send = new Button
                {
                    Text = "Send report",
                    DialogResult = DialogResult.OK,
                    Location = new System.Drawing.Point(252, 144),
                    Size = new System.Drawing.Size(75, 28)
                };
                var cancel = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new System.Drawing.Point(333, 144),
                    Size = new System.Drawing.Size(75, 28)
                };
                form.Controls.AddRange(new Control[] { label, box, send, cancel });
                form.AcceptButton = send;
                form.CancelButton = cancel;

                if (form.ShowDialog(owner) != DialogResult.OK || string.IsNullOrWhiteSpace(box.Text))
                    return;

                var text = box.Text;
                Task.Run(async () =>
                {
                    var ok = await SendUserReportAsync(owner, text).ConfigureAwait(false);
                    var ctl = owner as Control;
                    Action show = () => MessageBox.Show(owner,
                        ok
                            ? "Thank you — your bug report was sent."
                            : "Could not send the report. Check your internet connection and try again.",
                        "Report a bug",
                        MessageBoxButtons.OK,
                        ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                    if (ctl != null && ctl.InvokeRequired)
                        ctl.BeginInvoke(show);
                    else
                        show();
                });
            }
        }

        static async Task<bool> SendPayloadAsync(string exceptionType, string message, string stackTrace)
        {
            try
            {
                if (!await TruckDeckApiClient.RegisterInstallAsync().ConfigureAwait(false))
                    Log.Warn("Crash report skipped: install registration failed");

                var payload = new
                {
                    app_version = AssemblyHelper.Version,
                    os_version = Environment.OSVersion.ToString(),
                    exception_type = exceptionType,
                    message = message,
                    stack_trace = stackTrace,
                    log_tail = ReadLogTail(),
                };
                var res = await TruckDeckApiClient.PostSignedAsync("/api/v1/crash-reports", payload)
                    .ConfigureAwait(false);
                if (!res.IsSuccessStatusCode)
                {
                    var body = "";
                    try { body = await res.Content.ReadAsStringAsync().ConfigureAwait(false); } catch { /* ignore */ }
                    Log.Warn($"Crash report rejected: {(int)res.StatusCode} {body}");
                    return false;
                }

                Log.Info("Crash report sent successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("Crash report failed: " + ex.Message);
                return false;
            }
        }

        static string ReadLogTail()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TruckDeck.log");
                if (!File.Exists(path)) return "";
                var lines = File.ReadAllLines(path);
                return string.Join(Environment.NewLine, lines.Skip(Math.Max(0, lines.Length - MaxLogTailLines)));
            }
            catch
            {
                return "";
            }
        }
    }
}
