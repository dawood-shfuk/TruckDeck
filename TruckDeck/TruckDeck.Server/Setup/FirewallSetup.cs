using System;
using System.Reflection;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;

namespace Funbit.Ets.Telemetry.Server.Setup
{
    public class FirewallSetup : ISetup
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        SetupStatus _status;

        public FirewallSetup()
        {
            try
            {
                if (Settings.Instance.FirewallSetupHadErrors)
                {
                    _status = SetupStatus.Installed;
                }
                else
                {
                    Log.Info("Checking Firewall rules for ports " + HttpPortSetupHelper.PortListLabel + "...");
                    _status = HttpPortSetupHelper.HasAllFirewallRules()
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
                Log.Info("Adding Firewall rules for ports " + HttpPortSetupHelper.PortListLabel + "...");
                HttpPortSetupHelper.EnsureAllFirewallRules();
                _status = SetupStatus.Installed;
            }
            catch (Exception ex)
            {
                _status = SetupStatus.Failed;
                Log.Error(ex);
                Settings.Instance.FirewallSetupHadErrors = true;
                Settings.Instance.Save();
                throw new Exception("Cannot configure Windows Firewall." + Environment.NewLine +
                                    "If you are using some 3rd-party firewall please open TCP ports " +
                                    HttpPortSetupHelper.PortListLabel + " manually!", ex);
            }

            return _status;
        }

        public SetupStatus Uninstall(IWin32Window owner)
        {
            if (_status == SetupStatus.Uninstalled)
                return _status;

            try
            {
                Log.Info("Deleting Firewall rules for ports " + HttpPortSetupHelper.PortListLabel + "...");
                HttpPortSetupHelper.RemoveAllFirewallRules();
                _status = SetupStatus.Uninstalled;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                _status = SetupStatus.Failed;
                throw new Exception("Cannot configure Windows Firewall." + Environment.NewLine +
                                    "If you are using some 3rd-party firewall please close TCP ports " +
                                    HttpPortSetupHelper.PortListLabel + " manually!", ex);
            }
            return _status;
        }
    }
}
