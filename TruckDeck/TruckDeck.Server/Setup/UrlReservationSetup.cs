using System;
using System.Reflection;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;

namespace Funbit.Ets.Telemetry.Server.Setup
{
    public class UrlReservationSetup : ISetup
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        SetupStatus _status;

        public UrlReservationSetup()
        {
            try
            {
                if (Settings.Instance.UrlReservationSetupHadErrors)
                {
                    _status = SetupStatus.Installed;
                }
                else
                {
                    Log.Info("Checking ACL rule status for ports " + HttpPortSetupHelper.PortListLabel + "...");
                    _status = HttpPortSetupHelper.HasAllUrlReservations()
                        ? SetupStatus.Installed
                        : SetupStatus.Uninstalled;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                _status = SetupStatus.Failed;
            }
        }

        public SetupStatus Status => _status;

        public SetupStatus Install(IWin32Window owner)
        {
            if (_status == SetupStatus.Installed)
                return _status;

            try
            {
                Log.Info("Adding ACL rules for ports " + HttpPortSetupHelper.PortListLabel + "...");
                HttpPortSetupHelper.EnsureAllUrlReservations();
                _status = SetupStatus.Installed;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                _status = SetupStatus.Failed;
                Settings.Instance.UrlReservationSetupHadErrors = true;
                Settings.Instance.Save();
                throw;
            }
            return _status;
        }

        public SetupStatus Uninstall(IWin32Window owner)
        {
            if (_status == SetupStatus.Uninstalled)
                return _status;

            try
            {
                Log.Info("Deleting ACL rules for ports " + HttpPortSetupHelper.PortListLabel + "...");
                HttpPortSetupHelper.RemoveAllUrlReservations();
                _status = SetupStatus.Uninstalled;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                _status = SetupStatus.Failed;
            }
            return _status;
        }
    }
}
