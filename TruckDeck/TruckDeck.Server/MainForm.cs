using System;

using System.Configuration;

using System.Diagnostics;

using System.Drawing;

using System.IO;

using System.Linq;

using System.Net.Http;

using System.Reflection;

using System.Text;

using System.Windows.Forms;

using Funbit.Ets.Telemetry.Server.Bridges;

using Funbit.Ets.Telemetry.Server.Controllers;

using Funbit.Ets.Telemetry.Server.Data;

using Funbit.Ets.Telemetry.Server.Data.Reader;

using Funbit.Ets.Telemetry.Server.Helpers;

using Funbit.Ets.Telemetry.Server.Controls;

using Funbit.Ets.Telemetry.Server.Setup;

using Funbit.Ets.Telemetry.Server.Services;

using Microsoft.Owin.Hosting;



namespace Funbit.Ets.Telemetry.Server

{

    public partial class MainForm : Form

    {

        IDisposable _server;

        InputBridgeHost _inputBridge;

        string _dashboardUrl;

        string _apiUrl;

        NetworkInterfaceInfo _activeInterface;

        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);



        readonly HttpClient _broadcastHttpClient = new HttpClient();

        static readonly Encoding Utf8 = new UTF8Encoding(false);

        static readonly string BroadcastUrl = ConfigurationManager.AppSettings["BroadcastUrl"];

        static readonly string BroadcastUserId = Convert.ToBase64String(

            Utf8.GetBytes(ConfigurationManager.AppSettings["BroadcastUserId"] ?? ""));

        static readonly string BroadcastUserPassword = Convert.ToBase64String(

            Utf8.GetBytes(ConfigurationManager.AppSettings["BroadcastUserPassword"] ?? ""));

        static readonly int BroadcastRateInSeconds = Math.Min(Math.Max(1, 

            Convert.ToInt32(ConfigurationManager.AppSettings["BroadcastRate"])), 86400);

        static readonly bool UseTestTelemetryData = Convert.ToBoolean(

            ConfigurationManager.AppSettings["UseEts2TestTelemetryData"]);



        public MainForm()

        {

            InitializeComponent();

            ApplicationIconHelper.Apply(this, trayIcon);

            TruckDeckTheme.ApplyModern(this);

            UiBridge.Register(this);

        }



        void mapGeneratorButton_Click(object sender, EventArgs e)
        {
            using (var form = new MapGeneratorForm())
                form.ShowDialog(this);
        }

        void bridgeConfigButton_Click(object sender, EventArgs e)
        {
            using (var form = new BridgeConfigForm())
                form.ShowDialog(this);
        }

        void UpdateCabLinks(string ip)
        {
            _dashboardUrl = IpToEndpointUrl(ip) + Ets2AppController.TelemetryAppUriPath;
            _apiUrl = IpToEndpointUrl(ip) + Ets2TelemetryController.TelemetryApiUriPath;

            toolTip.SetToolTip(openDashboardButton, _dashboardUrl);
            toolTip.SetToolTip(openApiButton, _apiUrl);
        }

        static string IpToEndpointUrl(string host)
        {
            return $"http://{host}:{ConfigurationManager.AppSettings["Port"]}";
        }



        void Setup()

        {

            try

            {

                if (Program.UninstallMode && SetupManager.Steps.All(s => s.Status == SetupStatus.Uninstalled))

                {

                    MessageBox.Show(this, @"Server is not installed, nothing to uninstall.", @"Done",

                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Environment.Exit(0);

                }



                if (Program.UninstallMode || SetupManager.Steps.Any(s => s.Status != SetupStatus.Installed))

                {

                    var result = new SetupForm().ShowDialog(this);

                    if (Program.UninstallMode)

                        Environment.Exit(0);

                    if (result == DialogResult.Abort)

                        Environment.Exit(0);

                }



                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;

            }

            catch (Exception ex)

            {

                Log.Error(ex);

                ex.ShowAsMessageBox(this, @"Setup error");

            }

        }



        void Start()

        {

            try

            {

                var networkInterface = NetworkHelper.GetPreferredNetworkInterface();

                ApplyNetworkInterface(networkInterface, savePreference: true);

                networkRefreshTimer.Enabled = true;



                _server = WebApp.Start<Startup>(IpToEndpointUrl("+"));

                MapGenerationService.Instance.EnsureMapsActivated();

                TryEnsureBridgeUrlReservation();

                _inputBridge = new InputBridgeHost();

                _inputBridge.Start();

                if (!_inputBridge.IsRunning)
                {
                    Log.Warn("Input bridge failed to start — retrying after URL ACL check (port "
                             + HttpPortSetupHelper.InputBridgePort + ")");
                    TryEnsureBridgeUrlReservation();
                    _inputBridge.Start();
                }

                BridgeConfigService.Instance.RegisterHost(_inputBridge);



                statusUpdateTimer.Enabled = true;



                if (!string.IsNullOrEmpty(BroadcastUrl))

                {

                    _broadcastHttpClient.DefaultRequestHeaders.Add("X-UserId", BroadcastUserId);

                    _broadcastHttpClient.DefaultRequestHeaders.Add("X-UserPassword", BroadcastUserPassword);

                    broadcastTimer.Interval = BroadcastRateInSeconds * 1000;

                    broadcastTimer.Enabled = true;

                }



                trayIcon.Visible = true;

                Activate();

            }

            catch (Exception ex)

            {

                Log.Error(ex);

                ex.ShowAsMessageBox(this, @"Network error", MessageBoxIcon.Exclamation);

            }

        }



        void SetSimStatus(string text, Color color, bool pulsing)

        {

            statusLabel.Text = text;

            statusBeacon.StatusText = text;

            statusBeacon.StatusColor = color;

            statusBeacon.Pulsing = pulsing;

        }

        

        void MainForm_Load(object sender, EventArgs e)

        {

            Log.InfoFormat("Running application on {0} ({1}) {2}", Environment.OSVersion, 

                Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit",

                Program.UninstallMode ? "[UNINSTALL MODE]" : "");



            var version = AssemblyHelper.Version;

            Text = $@"TruckDeck {version}";

            truckDeckHeader.VersionText = version;

            footerLabel.Text = $@"Keep on trucking · v{version}";



            Setup();

            Start();

        }



        void MainForm_FormClosed(object sender, FormClosedEventArgs e)

        {

            _inputBridge?.Dispose();

            _server?.Dispose();

            trayIcon.Visible = false;

        }

    

        void closeToolStripMenuItem_Click(object sender, EventArgs e)

        {

            Close();

        }



        void showWindowToolStripMenuItem_Click(object sender, EventArgs e)

        {

            WindowState = FormWindowState.Normal;

            ShowInTaskbar = true;

            Activate();

        }



        void trayIcon_MouseDoubleClick(object sender, MouseEventArgs e)

        {

            showWindowToolStripMenuItem_Click(sender, e);

        }



        void statusUpdateTimer_Tick(object sender, EventArgs e)

        {

            try

            {

                if (UseTestTelemetryData)

                {

                    SetSimStatus(@"Connected to Ets2TestTelemetry.json", TruckDeckTheme.Connected, true);

                } 

                else if (Ets2ProcessHelper.IsEts2Running && ScsTelemetryDataReader.Instance.IsConnected)

                {

                    SetSimStatus(

                        $"Live link · {Ets2ProcessHelper.LastRunningGameName}",

                        TruckDeckTheme.Connected,

                        true);

                }

                else if (Ets2ProcessHelper.IsEts2Running)

                {

                    SetSimStatus(

                        $"Game running · waiting for telemetry ({Ets2ProcessHelper.LastRunningGameName})",

                        TruckDeckTheme.Running,

                        false);

                }

                else

                {

                    SetSimStatus(@"Simulator offline · start ETS2 or ATS", TruckDeckTheme.Disconnected, false);

                }

            }

            catch (Exception ex)

            {

                Log.Error(ex);

                ex.ShowAsMessageBox(this, @"Process error");

                statusUpdateTimer.Enabled = false;

            }

        }



        void ApplyNetworkInterface(NetworkInterfaceInfo networkInterface, bool savePreference)
        {
            if (networkInterface == null)
                return;

            _activeInterface = networkInterface;
            networkInterfaceTitleLabel.Text = networkInterface.AdapterSummary;
            ipAddressLabel.Text = networkInterface.Ip;
            UpdateCabLinks(networkInterface.Ip);

            if (savePreference)
            {
                Settings.Instance.DefaultNetworkInterfaceId = networkInterface.Id;
                Settings.Instance.Save();
            }
        }

        void RefreshNetworkInterface()
        {
            try
            {
                var preferred = NetworkHelper.GetPreferredNetworkInterface();
                if (_activeInterface != null
                    && preferred.Id == _activeInterface.Id
                    && preferred.Ip == _activeInterface.Ip)
                {
                    return;
                }

                ApplyNetworkInterface(preferred, savePreference: true);
            }
            catch (Exception ex)
            {
                Log.Warn("Network refresh failed: " + ex.Message);
            }
        }

        void networkRefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshNetworkInterface();
        }

        void openApiButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_apiUrl))
                ProcessHelper.OpenUrl(_apiUrl);
        }

        void openDonationPageLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var url = !string.IsNullOrWhiteSpace(_dashboardUrl)
                ? _dashboardUrl.TrimEnd('/') + "/downloads.html#donation-board"
                : "https://truckdeck.site/downloads";
            ProcessHelper.OpenUrl(url);
        }

        void fuelDonateButton_Click(object sender, EventArgs e)
        {
            ProcessHelper.OpenUrl("https://www.paypal.com/donate/?hosted_button_id=6ZS7K96DSKKF8");
        }

        void boostDonateButton_Click(object sender, EventArgs e)
        {
            ProcessHelper.OpenUrl("https://www.paypal.com/donate/?hosted_button_id=M5D5XMPXK2W4L");
        }

        void fleetDonateButton_Click(object sender, EventArgs e)
        {
            ProcessHelper.OpenUrl("https://www.paypal.com/donate/?hosted_button_id=M5D5XMPXK2W4L");
        }



        void openDashboardButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_dashboardUrl))
                ProcessHelper.OpenUrl(_dashboardUrl);
        }

        void minimizeLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)

        {

            WindowState = FormWindowState.Minimized;

        }



        void uninstallLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)

        {

            uninstallToolStripMenuItem_Click(sender, e);

        }

        

        void MainForm_Resize(object sender, EventArgs e)

        {

            ShowInTaskbar = WindowState != FormWindowState.Minimized;

            if (!ShowInTaskbar && trayIcon.Tag == null)

            {

                trayIcon.ShowBalloonTip(1000, @"TruckDeck", @"Double-click the tray icon to restore.", ToolTipIcon.Info);

                trayIcon.Tag = "Already shown";

            }

        }



        async void broadcastTimer_Tick(object sender, EventArgs e)

        {

            try

            {

                broadcastTimer.Enabled = false;

                await _broadcastHttpClient.PostAsJsonAsync(BroadcastUrl, ScsTelemetryDataReader.Instance.Read());

            }

            catch (Exception ex)

            {

                Log.Error(ex);

            }

            broadcastTimer.Enabled = true;

        }

        

        void uninstallToolStripMenuItem_Click(object sender, EventArgs e)

        {

            var confirm = MessageBox.Show(this,

                @"This removes TruckDeck, cleans up telemetry plugins, firewall rules, and URL reservations." + Environment.NewLine + Environment.NewLine +

                @"Continue?",

                @"Uninstall TruckDeck",

                MessageBoxButtons.YesNo,

                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)

                return;



            try

            {

                _inputBridge?.Dispose();

                _server?.Dispose();

            }

            catch (Exception ex)

            {

                Log.Warn("Could not stop server before uninstall", ex);

            }



            string error;

            if (WindowsUninstallHelper.TryLaunchUninstaller(out error))

            {

                Application.Exit();

                return;

            }



            var portable = MessageBox.Show(this,

                (error ?? @"No Windows installer entry was found.") + Environment.NewLine + Environment.NewLine +

                @"Run component cleanup only? (Program files will remain.)",

                @"Portable uninstall",

                MessageBoxButtons.YesNo,

                MessageBoxIcon.Warning);

            if (portable != DialogResult.Yes)

                return;



            try

            {

                var exeFileName = Process.GetCurrentProcess().MainModule.FileName;

                Process.Start(new ProcessStartInfo

                {

                    FileName = exeFileName,

                    Arguments = "-uninstall -interactive",

                    Verb = "runas",

                    UseShellExecute = true,

                    WorkingDirectory = Path.GetDirectoryName(exeFileName)

                });

            }

            catch (Exception ex)

            {

                Log.Error(ex);

                ex.ShowAsMessageBox(this, @"Uninstall error");

                return;

            }

            Application.Exit();

        }

        void TryEnsureBridgeUrlReservation()
        {
            if (!HttpPortSetupHelper.HasUrlReservation(HttpPortSetupHelper.InputBridgePort))
            {
                if (!Uac.IsProcessElevated())
                {
                    Log.Warn("Input bridge URL ACL missing for port " + HttpPortSetupHelper.InputBridgePort
                             + ". Run TruckDeck as Administrator once, or re-run setup.");
                    return;
                }

                try
                {
                    HttpPortSetupHelper.EnsureUrlReservation(HttpPortSetupHelper.InputBridgePort);
                    HttpPortSetupHelper.EnsureFirewallRule(HttpPortSetupHelper.InputBridgePort);
                    Log.Info("Added missing input bridge URL ACL / firewall rule for port "
                             + HttpPortSetupHelper.InputBridgePort);
                }
                catch (Exception ex)
                {
                    Log.Warn("Could not add input bridge URL ACL: " + ex.Message);
                }
            }
        }

    }

}


