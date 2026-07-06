using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;
using Funbit.Ets.Telemetry.Server.Setup;

namespace Funbit.Ets.Telemetry.Server
{
    static class UninstallBootstrap
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(UninstallBootstrap));

        public static int RunSilent()
        {
            try
            {
                if (Ets2ProcessHelper.IsEts2Running)
                {
                    MessageBox.Show(
                        @"Close Euro Truck Simulator 2 / American Truck Simulator before uninstalling TruckDeck.",
                        @"TruckDeck Uninstall",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return 1;
                }

                if (SetupManager.Steps.All(s => s.Status == SetupStatus.Uninstalled))
                {
                    Log.Info("TruckDeck silent uninstall: nothing to remove.");
                    Settings.Clear();
                    return 0;
                }

                Log.Info("TruckDeck silent uninstall starting...");

                var failed = false;
                foreach (var step in SetupManager.Steps)
                {
                    try
                    {
                        var status = step.Uninstall(null);
                        if (status == SetupStatus.Failed)
                        {
                            Log.Error("Uninstall step failed: " + step.GetType().Name);
                            failed = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Uninstall step error: " + step.GetType().Name, ex);
                        failed = true;
                    }
                }

                Settings.Clear();
                Log.Info(failed
                    ? "TruckDeck silent uninstall completed with errors."
                    : "TruckDeck silent uninstall completed.");
                return failed ? 1 : 0;
            }
            catch (Exception ex)
            {
                Log.Error("Silent uninstall failed", ex);
                return 1;
            }
        }
    }
}
