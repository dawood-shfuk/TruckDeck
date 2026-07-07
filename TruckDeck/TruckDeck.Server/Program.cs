using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;
using Funbit.Ets.Telemetry.Server.Services;

namespace Funbit.Ets.Telemetry.Server
{
    static class Program
    {
        [DllImport("kernel32.dll", EntryPoint = "CreateMutexA")]
        private static extern int CreateMutex(int lpMutexAttributes, int bInitialOwner, string lpName);
        [DllImport("kernel32.dll")]
        private static extern int GetLastError();
        private const int ErrorAlreadyExists = 183;

        public static bool UninstallMode;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => LogCrash(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    LogCrash(ex);
            };

            // check if another instance is running
            CreateMutex(0, -1,
                Uac.IsProcessElevated()
                    ? "TruckDeck_8F63CCBE353DE22BD1A86308AD675001_UAC"
                    : "TruckDeck_8F63CCBE353DE22BD1A86308AD675001");
            bool bAnotherInstanceRunning = GetLastError() == ErrorAlreadyExists;
            if (bAnotherInstanceRunning)
            {
                MessageBox.Show(@"Another TruckDeck instance is already running!", @"Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            log4net.Config.XmlConfigurator.Configure();
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            UninstallMode = args.Any(a => string.Equals(a.Trim(), "-uninstall", StringComparison.OrdinalIgnoreCase));
            var uninstallInteractive = args.Any(a => string.Equals(a.Trim(), "-interactive", StringComparison.OrdinalIgnoreCase));

            if (args.Any(a => string.Equals(a.Trim(), "-install", StringComparison.OrdinalIgnoreCase)))
            {
                if (!Uac.IsProcessElevated())
                {
                    Uac.RestartElevated();
                    return;
                }
                Environment.Exit(InstallBootstrap.RunSilent(args));
                return;
            }

            if (UninstallMode)
            {
                if (!Uac.IsProcessElevated())
                {
                    Uac.RestartElevated();
                    return;
                }

                if (uninstallInteractive)
                    Application.Run(new MainForm());
                else
                    Environment.Exit(UninstallBootstrap.RunSilent());
                return;
            }

            if (!Uac.IsProcessElevated())
            {
                MessageBox.Show(
                    @"TruckDeck must run as Administrator for WSL map generation and firewall setup.",
                    @"Administrator required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                try
                {
                    Uac.RestartElevated();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        @"Could not restart elevated: " + ex.Message,
                        @"Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                return;
            }

            Application.Run(new MainForm());
        }

        static void LogCrash(Exception ex)
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TruckDeck-crash.log");
                File.AppendAllText(path, DateTime.Now + Environment.NewLine + ex + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
                // ignore
            }

            try
            {
                if (ClientState.Instance.CrashReportingEnabled)
                {
                    CrashReportService.ReportAsync(ex);
                    return;
                }

                var result = MessageBox.Show(
                    "TruckDeck encountered an error. Send an anonymous crash report to the developer?",
                    "TruckDeck",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    ClientState.Instance.CrashReportingEnabled = true;
                    ClientState.Instance.Save();
                    CrashReportService.ReportAsync(ex);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
