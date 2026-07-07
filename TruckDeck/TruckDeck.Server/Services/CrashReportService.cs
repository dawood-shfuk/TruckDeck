using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Funbit.Ets.Telemetry.Server.Helpers;

namespace Funbit.Ets.Telemetry.Server.Services
{
    public static class CrashReportService
    {
        const int MaxLogTailLines = 80;

        public static void ReportAsync(Exception ex)
        {
            if (!ClientState.Instance.CrashReportingEnabled)
                return;

            Task.Run(async () =>
            {
                try
                {
                    await TruckDeckApiClient.RegisterInstallAsync();
                    var payload = new
                    {
                        app_version = AssemblyHelper.Version,
                        os_version = Environment.OSVersion.ToString(),
                        exception_type = ex.GetType().FullName,
                        message = ex.Message,
                        stack_trace = ex.ToString(),
                        log_tail = ReadLogTail(),
                    };
                    await TruckDeckApiClient.PostSignedAsync("/api/v1/crash-reports", payload);
                }
                catch
                {
                    // silent
                }
            });
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
