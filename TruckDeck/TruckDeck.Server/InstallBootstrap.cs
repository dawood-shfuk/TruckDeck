using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;
using Funbit.Ets.Telemetry.Server.Setup;

namespace Funbit.Ets.Telemetry.Server
{
    static class InstallBootstrap
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(InstallBootstrap));

        public static int RunSilent(string[] args)
        {
            try
            {
                ApplyPathArgs(args);
                Log.Info("TruckDeck silent install starting...");

                foreach (var step in SetupManager.Steps)
                {
                    var status = step.Install(null);
                    if (status == SetupStatus.Failed)
                    {
                        Log.Error("Setup step failed: " + step.GetType().Name);
                        return 1;
                    }
                }

                Log.Info("TruckDeck silent install completed.");
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error("Silent install failed", ex);
                return 1;
            }
        }

        static void ApplyPathArgs(string[] args)
        {
            if (args.Any(a => string.Equals(a.Trim(), "-frominstaller", StringComparison.OrdinalIgnoreCase)))
                LoadPathsFromInstallerIni();

            string ets2 = ParsePathArg(args, "-ets2:");
            string ats = ParsePathArg(args, "-ats:");
            if (ets2 != null)
                Settings.Instance.Ets2GamePath = ets2;
            if (ats != null)
                Settings.Instance.AtsGamePath = ats;
            if (ets2 != null || ats != null)
                Settings.Instance.Save();
        }

        static void LoadPathsFromInstallerIni()
        {
            var ini = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".truckdeck-install.ini");
            if (!File.Exists(ini))
            {
                Log.Warn("Installer ini not found: " + ini);
                return;
            }

            foreach (var line in File.ReadAllLines(ini))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("ets2=", StringComparison.OrdinalIgnoreCase))
                    Settings.Instance.Ets2GamePath = trimmed.Substring(5).Trim();
                else if (trimmed.StartsWith("ats=", StringComparison.OrdinalIgnoreCase))
                    Settings.Instance.AtsGamePath = trimmed.Substring(4).Trim();
            }

            Settings.Instance.Save();
            try { File.Delete(ini); }
            catch (Exception ex) { Log.Warn("Could not delete installer ini", ex); }
        }

        static string ParsePathArg(string[] args, string prefix)
        {
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var value = arg.Substring(prefix.Length).Trim().Trim('"');
                    if (string.Equals(value, "N/A", StringComparison.OrdinalIgnoreCase))
                        return "N/A";
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }

                if (string.Equals(arg, prefix.TrimEnd(':'), StringComparison.OrdinalIgnoreCase)
                    && i + 1 < args.Length)
                {
                    var value = args[i + 1].Trim().Trim('"');
                    if (string.Equals(value, "N/A", StringComparison.OrdinalIgnoreCase))
                        return "N/A";
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            return null;
        }
    }
}
