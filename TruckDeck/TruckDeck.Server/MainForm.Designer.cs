namespace Funbit.Ets.Telemetry.Server

{

    partial class MainForm

    {

        private System.ComponentModel.IContainer components = null;



        protected override void Dispose(bool disposing)

        {

            if (disposing && (components != null))

            {

                components.Dispose();

            }

            base.Dispose(disposing);

        }



        #region Windows Form Designer generated code



        private void InitializeComponent()

        {

            this.components = new System.ComponentModel.Container();

            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));

            this.trayIcon = new System.Windows.Forms.NotifyIcon(this.components);

            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);

            this.showWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            this.uninstallToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();

            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            this.statusUpdateTimer = new System.Windows.Forms.Timer(this.components);

            this.toolTip = new System.Windows.Forms.ToolTip(this.components);

            this.broadcastTimer = new System.Windows.Forms.Timer(this.components);

            this.networkRefreshTimer = new System.Windows.Forms.Timer(this.components);

            this.truckDeckHeader = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckHeader();

            this.contentPanel = new System.Windows.Forms.Panel();

            this.linksCard = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckCard();

            this.openApiButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();

            this.openDashboardButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();

            this.apiEndpointUrlTitleLabel = new System.Windows.Forms.Label();

            this.appUrlTitleLabel = new System.Windows.Forms.Label();

            this.mapGeneratorButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();

            this.mapGeneratorTitleLabel = new System.Windows.Forms.Label();

            this.bridgeConfigButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();

            this.bridgeConfigTitleLabel = new System.Windows.Forms.Label();

            this.networkCard = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckCard();

            this.ipAddressLabel = new System.Windows.Forms.Label();

            this.serverIpTitleLabel = new System.Windows.Forms.Label();

            this.networkInterfaceTitleLabel = new System.Windows.Forms.Label();

            this.feedbackCard = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckCard();

            this.feedbackTitleLabel = new System.Windows.Forms.Label();

            this.feedbackLedeLabel = new System.Windows.Forms.Label();

            this.feedbackRateButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();

            this.feedbackReviewsLink = new System.Windows.Forms.LinkLabel();

            this.statusCard = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckCard();

            this.statusBeacon = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckStatusBeacon();

            this.statusLabel = new System.Windows.Forms.Label();

            this.actionPanel = new System.Windows.Forms.Panel();

            this.donationCard = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckCard();

            this.openDonationPageLink = new System.Windows.Forms.LinkLabel();

            this.fleetDonateButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();

            this.boostDonateButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();

            this.fuelDonateButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();

            this.supportLedeLabel = new System.Windows.Forms.Label();

            this.supportTitleLabel = new System.Windows.Forms.Label();

            this.uninstallLink = new System.Windows.Forms.LinkLabel();

            this.minimizeLink = new System.Windows.Forms.LinkLabel();

            this.footerLabel = new System.Windows.Forms.Label();

            this.contextMenuStrip.SuspendLayout();

            this.contentPanel.SuspendLayout();

            this.linksCard.SuspendLayout();

            this.networkCard.SuspendLayout();

            this.feedbackCard.SuspendLayout();

            this.statusCard.SuspendLayout();

            this.donationCard.SuspendLayout();

            this.actionPanel.SuspendLayout();

            this.SuspendLayout();

            // 

            // trayIcon

            // 

            this.trayIcon.BalloonTipTitle = "TruckDeck is running...";

            this.trayIcon.ContextMenuStrip = this.contextMenuStrip;

            this.trayIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("trayIcon.Icon")));

            this.trayIcon.Text = "TruckDeck is running...";

            this.trayIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.trayIcon_MouseDoubleClick);

            // 

            // contextMenuStrip

            // 

            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {

            this.showWindowToolStripMenuItem,

            this.uninstallToolStripMenuItem,

            this.toolStripSeparator1,

            this.closeToolStripMenuItem});

            this.contextMenuStrip.Name = "contextMenuStrip";

            this.contextMenuStrip.Size = new System.Drawing.Size(181, 76);

            // 

            // showWindowToolStripMenuItem

            // 

            this.showWindowToolStripMenuItem.Name = "showWindowToolStripMenuItem";

            this.showWindowToolStripMenuItem.Size = new System.Drawing.Size(180, 22);

            this.showWindowToolStripMenuItem.Text = "Show TruckDeck";

            this.showWindowToolStripMenuItem.Click += new System.EventHandler(this.showWindowToolStripMenuItem_Click);

            // 

            // uninstallToolStripMenuItem

            // 

            this.uninstallToolStripMenuItem.Name = "uninstallToolStripMenuItem";

            this.uninstallToolStripMenuItem.Size = new System.Drawing.Size(180, 22);

            this.uninstallToolStripMenuItem.Text = "Uninstall";

            this.uninstallToolStripMenuItem.Click += new System.EventHandler(this.uninstallToolStripMenuItem_Click);

            // 

            // toolStripSeparator1

            // 

            this.toolStripSeparator1.Name = "toolStripSeparator1";

            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);

            // 

            // closeToolStripMenuItem

            // 

            this.closeToolStripMenuItem.Image = global::Funbit.Ets.Telemetry.Server.Properties.Resources.CloseIcon;

            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";

            this.closeToolStripMenuItem.Size = new System.Drawing.Size(180, 22);

            this.closeToolStripMenuItem.Text = "Close";

            this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);

            // 

            // statusUpdateTimer

            // 

            this.statusUpdateTimer.Interval = 1000;

            this.statusUpdateTimer.Tick += new System.EventHandler(this.statusUpdateTimer_Tick);

            // 

            // toolTip

            // 

            this.toolTip.AutomaticDelay = 250;

            this.toolTip.AutoPopDelay = 6000;

            this.toolTip.InitialDelay = 250;

            this.toolTip.ReshowDelay = 50;

            this.toolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;

            // 

            // broadcastTimer

            // 

            this.broadcastTimer.Interval = 1000;

            this.broadcastTimer.Tick += new System.EventHandler(this.broadcastTimer_Tick);

            // 

            // networkRefreshTimer

            // 

            this.networkRefreshTimer.Interval = 8000;

            this.networkRefreshTimer.Tick += new System.EventHandler(this.networkRefreshTimer_Tick);

            // 

            // truckDeckHeader

            // 

            this.truckDeckHeader.Dock = System.Windows.Forms.DockStyle.Top;

            this.truckDeckHeader.Location = new System.Drawing.Point(0, 0);

            this.truckDeckHeader.Name = "truckDeckHeader";

            this.truckDeckHeader.Size = new System.Drawing.Size(460, 92);

            this.truckDeckHeader.TabIndex = 0;

            // 

            // contentPanel

            // 

            this.contentPanel.AutoScroll = true;

            this.contentPanel.Controls.Add(this.donationCard);

            this.contentPanel.Controls.Add(this.linksCard);

            this.contentPanel.Controls.Add(this.feedbackCard);

            this.contentPanel.Controls.Add(this.networkCard);

            this.contentPanel.Controls.Add(this.statusCard);

            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;

            this.contentPanel.Location = new System.Drawing.Point(0, 92);

            this.contentPanel.Name = "contentPanel";

            this.contentPanel.Padding = new System.Windows.Forms.Padding(16, 12, 16, 8);

            this.contentPanel.Size = new System.Drawing.Size(460, 564);

            this.contentPanel.TabIndex = 1;

            // 

            // linksCard

            // 

            this.linksCard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 

            | System.Windows.Forms.AnchorStyles.Right)));

            this.linksCard.CardTitle = "CAB LINKS";

            this.linksCard.Controls.Add(this.bridgeConfigButton);

            this.linksCard.Controls.Add(this.bridgeConfigTitleLabel);

            this.linksCard.Controls.Add(this.mapGeneratorButton);

            this.linksCard.Controls.Add(this.mapGeneratorTitleLabel);

            this.linksCard.Controls.Add(this.openApiButton);

            this.linksCard.Controls.Add(this.apiEndpointUrlTitleLabel);

            this.linksCard.Controls.Add(this.openDashboardButton);

            this.linksCard.Controls.Add(this.appUrlTitleLabel);

            this.linksCard.Location = new System.Drawing.Point(16, 348);

            this.linksCard.Name = "linksCard";

            this.linksCard.Size = new System.Drawing.Size(428, 200);

            this.linksCard.TabIndex = 3;

            // 

            // openApiButton

            // 

            this.openApiButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 

            | System.Windows.Forms.AnchorStyles.Right)));

            this.openApiButton.Location = new System.Drawing.Point(108, 66);

            this.openApiButton.Name = "openApiButton";

            this.openApiButton.Primary = false;

            this.openApiButton.Size = new System.Drawing.Size(304, 32);

            this.openApiButton.TabIndex = 3;

            this.openApiButton.Text = "OPEN REST API";

            this.toolTip.SetToolTip(this.openApiButton, "Open the REST telemetry API in your browser");

            this.openApiButton.Click += new System.EventHandler(this.openApiButton_Click);

            // 

            // apiEndpointUrlTitleLabel

            // 

            this.apiEndpointUrlTitleLabel.AutoSize = true;

            this.apiEndpointUrlTitleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9f, System.Drawing.FontStyle.Bold);

            this.apiEndpointUrlTitleLabel.Location = new System.Drawing.Point(19, 74);

            this.apiEndpointUrlTitleLabel.Name = "apiEndpointUrlTitleLabel";

            this.apiEndpointUrlTitleLabel.Size = new System.Drawing.Size(83, 15);

            this.apiEndpointUrlTitleLabel.TabIndex = 2;

            this.apiEndpointUrlTitleLabel.Text = "REST API base";

            // 

            // openDashboardButton

            // 

            this.openDashboardButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 

            | System.Windows.Forms.AnchorStyles.Right)));

            this.openDashboardButton.Location = new System.Drawing.Point(108, 28);

            this.openDashboardButton.Name = "openDashboardButton";

            this.openDashboardButton.Size = new System.Drawing.Size(304, 32);

            this.openDashboardButton.TabIndex = 1;

            this.openDashboardButton.Text = "OPEN DASHBOARD";

            this.toolTip.SetToolTip(this.openDashboardButton, "Open the HTML5 dashboard in your browser");

            this.openDashboardButton.Click += new System.EventHandler(this.openDashboardButton_Click);

            // 

            // appUrlTitleLabel

            // 

            this.appUrlTitleLabel.AutoSize = true;

            this.appUrlTitleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9f, System.Drawing.FontStyle.Bold);

            this.appUrlTitleLabel.Location = new System.Drawing.Point(19, 36);

            this.appUrlTitleLabel.Name = "appUrlTitleLabel";

            this.appUrlTitleLabel.Size = new System.Drawing.Size(70, 15);

            this.appUrlTitleLabel.TabIndex = 0;

            this.appUrlTitleLabel.Text = "Dashboard";

            // 

            // mapGeneratorTitleLabel

            // 

            this.mapGeneratorTitleLabel.AutoSize = true;

            this.mapGeneratorTitleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9f, System.Drawing.FontStyle.Bold);

            this.mapGeneratorTitleLabel.Location = new System.Drawing.Point(19, 108);

            this.mapGeneratorTitleLabel.Name = "mapGeneratorTitleLabel";

            this.mapGeneratorTitleLabel.Size = new System.Drawing.Size(90, 15);

            this.mapGeneratorTitleLabel.TabIndex = 4;

            this.mapGeneratorTitleLabel.Text = "Map Generator";

            // 

            // mapGeneratorButton

            // 

            this.mapGeneratorButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 

            | System.Windows.Forms.AnchorStyles.Right)));

            this.mapGeneratorButton.Location = new System.Drawing.Point(108, 100);

            this.mapGeneratorButton.Name = "mapGeneratorButton";

            this.mapGeneratorButton.Size = new System.Drawing.Size(304, 32);

            this.mapGeneratorButton.TabIndex = 5;

            this.mapGeneratorButton.Text = "OPEN MAP GENERATOR";

            this.toolTip.SetToolTip(this.mapGeneratorButton, "Build PMTiles from your game install (WSL + tippecanoe)");

            this.mapGeneratorButton.Click += new System.EventHandler(this.mapGeneratorButton_Click);

            // 

            // bridgeConfigTitleLabel

            // 

            this.bridgeConfigTitleLabel.AutoSize = true;

            this.bridgeConfigTitleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9f, System.Drawing.FontStyle.Bold);

            this.bridgeConfigTitleLabel.Location = new System.Drawing.Point(19, 146);

            this.bridgeConfigTitleLabel.Name = "bridgeConfigTitleLabel";

            this.bridgeConfigTitleLabel.Size = new System.Drawing.Size(82, 15);

            this.bridgeConfigTitleLabel.TabIndex = 6;

            this.bridgeConfigTitleLabel.Text = "Input Bridge";

            // 

            // bridgeConfigButton

            // 

            this.bridgeConfigButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 

            | System.Windows.Forms.AnchorStyles.Right)));

            this.bridgeConfigButton.Location = new System.Drawing.Point(108, 138);

            this.bridgeConfigButton.Name = "bridgeConfigButton";

            this.bridgeConfigButton.Size = new System.Drawing.Size(304, 32);

            this.bridgeConfigButton.TabIndex = 7;

            this.bridgeConfigButton.Text = "OPEN BRIDGE CONFIG";

            this.toolTip.SetToolTip(this.bridgeConfigButton, "Map dashboard buttons to in-game keys and joystick screen-cycle");

            this.bridgeConfigButton.Click += new System.EventHandler(this.bridgeConfigButton_Click);

            // 

            // networkCard

            // 

            this.networkCard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 

            | System.Windows.Forms.AnchorStyles.Right)));

            this.networkCard.CardTitle = "NETWORK";

            this.networkCard.Controls.Add(this.ipAddressLabel);

            this.networkCard.Controls.Add(this.serverIpTitleLabel);

            this.networkCard.Controls.Add(this.networkInterfaceTitleLabel);

            this.networkCard.Location = new System.Drawing.Point(16, 96);

            this.networkCard.Name = "networkCard";

            this.networkCard.Size = new System.Drawing.Size(428, 82);

            this.networkCard.TabIndex = 1;

            // 

            // ipAddressLabel

            // 

            this.ipAddressLabel.AutoSize = true;

            this.ipAddressLabel.Font = new System.Drawing.Font("Consolas", 12f, System.Drawing.FontStyle.Bold);

            this.ipAddressLabel.Location = new System.Drawing.Point(108, 52);

            this.ipAddressLabel.Name = "ipAddressLabel";

            this.ipAddressLabel.Size = new System.Drawing.Size(126, 19);

            this.ipAddressLabel.TabIndex = 3;

            this.ipAddressLabel.Text = "111.222.333.444";

            this.toolTip.SetToolTip(this.ipAddressLabel, "Auto-detected LAN IP — use on your phone or tablet");

            // 

            // serverIpTitleLabel

            // 

            this.serverIpTitleLabel.AutoSize = true;

            this.serverIpTitleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9f, System.Drawing.FontStyle.Bold);

            this.serverIpTitleLabel.Location = new System.Drawing.Point(19, 54);

            this.serverIpTitleLabel.Name = "serverIpTitleLabel";

            this.serverIpTitleLabel.Size = new System.Drawing.Size(58, 15);

            this.serverIpTitleLabel.TabIndex = 2;

            this.serverIpTitleLabel.Text = "Server IP";

            // 

            // networkInterfaceTitleLabel

            // 

            this.networkInterfaceTitleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 

            | System.Windows.Forms.AnchorStyles.Right)));

            this.networkInterfaceTitleLabel.Font = new System.Drawing.Font("Segoe UI", 9f);

            this.networkInterfaceTitleLabel.Location = new System.Drawing.Point(19, 36);

            this.networkInterfaceTitleLabel.Name = "networkInterfaceTitleLabel";

            this.networkInterfaceTitleLabel.Size = new System.Drawing.Size(393, 15);

            this.networkInterfaceTitleLabel.TabIndex = 0;

            this.networkInterfaceTitleLabel.Text = "Ethernet · DHCP";

            // 

            // feedbackCard

            // 

            this.feedbackCard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 

            | System.Windows.Forms.AnchorStyles.Right)));

            this.feedbackCard.CardTitle = "FEEDBACK";

            this.feedbackCard.Controls.Add(this.feedbackReviewsLink);

            this.feedbackCard.Controls.Add(this.feedbackRateButton);

            this.feedbackCard.Controls.Add(this.feedbackLedeLabel);

            this.feedbackCard.Controls.Add(this.feedbackTitleLabel);

            this.feedbackCard.Location = new System.Drawing.Point(16, 188);

            this.feedbackCard.Name = "feedbackCard";

            this.feedbackCard.Size = new System.Drawing.Size(428, 150);

            this.feedbackCard.TabIndex = 2;

            // 

            // feedbackTitleLabel

            // 

            this.feedbackTitleLabel.AutoSize = true;

            this.feedbackTitleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);

            this.feedbackTitleLabel.Location = new System.Drawing.Point(19, 34);

            this.feedbackTitleLabel.Name = "feedbackTitleLabel";

            this.feedbackTitleLabel.Size = new System.Drawing.Size(280, 17);

            this.feedbackTitleLabel.TabIndex = 0;

            this.feedbackTitleLabel.Text = "💬 Your feedback is very important";

            // 

            // feedbackLedeLabel

            // 

            this.feedbackLedeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 

            | System.Windows.Forms.AnchorStyles.Right)));

            this.feedbackLedeLabel.Font = new System.Drawing.Font("Segoe UI", 8.25F);

            this.feedbackLedeLabel.Location = new System.Drawing.Point(19, 54);

            this.feedbackLedeLabel.Name = "feedbackLedeLabel";

            this.feedbackLedeLabel.Size = new System.Drawing.Size(393, 30);

            this.feedbackLedeLabel.TabIndex = 1;

            this.feedbackLedeLabel.Text = "A quick rating on truckdeck.site helps other truckers find the app — it takes 30 se" +
    "conds.";

            // 

            // feedbackRateButton

            // 

            this.feedbackRateButton.Font = new System.Drawing.Font("Segoe UI Semibold", 8.25F, System.Drawing.FontStyle.Bold);

            this.feedbackRateButton.Location = new System.Drawing.Point(19, 92);

            this.feedbackRateButton.Name = "feedbackRateButton";

            this.feedbackRateButton.Primary = true;

            this.feedbackRateButton.Size = new System.Drawing.Size(180, 32);

            this.feedbackRateButton.TabIndex = 2;

            this.feedbackRateButton.Text = "★ RATE TRUCKDECK";

            this.feedbackRateButton.Click += new System.EventHandler(this.feedbackRateButton_Click);

            // 

            // feedbackReviewsLink

            // 

            this.feedbackReviewsLink.AutoSize = true;

            this.feedbackReviewsLink.Font = new System.Drawing.Font("Segoe UI", 8.25F);

            this.feedbackReviewsLink.Location = new System.Drawing.Point(19, 132);

            this.feedbackReviewsLink.Name = "feedbackReviewsLink";

            this.feedbackReviewsLink.Size = new System.Drawing.Size(151, 13);

            this.feedbackReviewsLink.TabIndex = 3;

            this.feedbackReviewsLink.TabStop = true;

            this.feedbackReviewsLink.Text = "See what other truckers say";

            this.feedbackReviewsLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.feedbackReviewsLink_LinkClicked);

            // 

            // statusCard

            // 

            this.statusCard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 

            | System.Windows.Forms.AnchorStyles.Right)));

            this.statusCard.CardTitle = "SIMULATOR";

            this.statusCard.Controls.Add(this.statusBeacon);

            this.statusCard.Controls.Add(this.statusLabel);

            this.statusCard.Location = new System.Drawing.Point(16, 12);

            this.statusCard.Name = "statusCard";

            this.statusCard.Size = new System.Drawing.Size(428, 72);

            this.statusCard.TabIndex = 0;

            // 

            // statusBeacon

            // 

            this.statusBeacon.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 

            | System.Windows.Forms.AnchorStyles.Right)));

            this.statusBeacon.Location = new System.Drawing.Point(12, 28);

            this.statusBeacon.Name = "statusBeacon";

            this.statusBeacon.Size = new System.Drawing.Size(404, 36);

            this.statusBeacon.TabIndex = 0;

            // 

            // statusLabel

            // 

            this.statusLabel.AutoSize = true;

            this.statusLabel.Location = new System.Drawing.Point(-100, -100);

            this.statusLabel.Name = "statusLabel";

            this.statusLabel.Size = new System.Drawing.Size(0, 13);

            this.statusLabel.TabIndex = 1;

            this.statusLabel.Visible = false;

            // 

            // donationCard

            // 

            this.donationCard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 

            | System.Windows.Forms.AnchorStyles.Right)));

            this.donationCard.CardTitle = "SUPPORT";

            this.donationCard.Controls.Add(this.openDonationPageLink);

            this.donationCard.Controls.Add(this.fleetDonateButton);

            this.donationCard.Controls.Add(this.boostDonateButton);

            this.donationCard.Controls.Add(this.fuelDonateButton);

            this.donationCard.Controls.Add(this.supportLedeLabel);

            this.donationCard.Controls.Add(this.supportTitleLabel);

            this.donationCard.Location = new System.Drawing.Point(16, 558);

            this.donationCard.Name = "donationCard";

            this.donationCard.Size = new System.Drawing.Size(428, 168);

            this.donationCard.TabIndex = 4;

            // 

            // supportTitleLabel

            // 

            this.supportTitleLabel.AutoSize = true;

            this.supportTitleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);

            this.supportTitleLabel.Location = new System.Drawing.Point(19, 34);

            this.supportTitleLabel.Name = "supportTitleLabel";

            this.supportTitleLabel.Size = new System.Drawing.Size(220, 17);

            this.supportTitleLabel.TabIndex = 0;

            this.supportTitleLabel.Text = "Support TruckDeck development";

            // 

            // supportLedeLabel

            // 

            this.supportLedeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 

            | System.Windows.Forms.AnchorStyles.Right)));

            this.supportLedeLabel.Font = new System.Drawing.Font("Segoe UI", 8.25F);

            this.supportLedeLabel.Location = new System.Drawing.Point(19, 54);

            this.supportLedeLabel.Name = "supportLedeLabel";

            this.supportLedeLabel.Size = new System.Drawing.Size(393, 30);

            this.supportLedeLabel.TabIndex = 1;

            this.supportLedeLabel.Text = "TruckDeck is free for the community. Fuel the rig, boost the tech, or join the fleet — every contribution helps.";

            // 

            // fuelDonateButton

            // 

            this.fuelDonateButton.Font = new System.Drawing.Font("Segoe UI Semibold", 8.25F, System.Drawing.FontStyle.Bold);

            this.fuelDonateButton.Location = new System.Drawing.Point(19, 92);

            this.fuelDonateButton.Name = "fuelDonateButton";

            this.fuelDonateButton.Primary = false;

            this.fuelDonateButton.Size = new System.Drawing.Size(124, 30);

            this.fuelDonateButton.TabIndex = 2;

            this.fuelDonateButton.Text = "FUEL THE RIG";

            this.fuelDonateButton.Click += new System.EventHandler(this.fuelDonateButton_Click);

            // 

            // boostDonateButton

            // 

            this.boostDonateButton.Font = new System.Drawing.Font("Segoe UI Semibold", 8.25F, System.Drawing.FontStyle.Bold);

            this.boostDonateButton.Location = new System.Drawing.Point(152, 92);

            this.boostDonateButton.Name = "boostDonateButton";

            this.boostDonateButton.Primary = false;

            this.boostDonateButton.Size = new System.Drawing.Size(124, 30);

            this.boostDonateButton.TabIndex = 3;

            this.boostDonateButton.Text = "BOOST THE TECH";

            this.boostDonateButton.Click += new System.EventHandler(this.boostDonateButton_Click);

            // 

            // fleetDonateButton

            // 

            this.fleetDonateButton.Font = new System.Drawing.Font("Segoe UI Semibold", 8.25F, System.Drawing.FontStyle.Bold);

            this.fleetDonateButton.Location = new System.Drawing.Point(285, 92);

            this.fleetDonateButton.Name = "fleetDonateButton";

            this.fleetDonateButton.Primary = false;

            this.fleetDonateButton.Size = new System.Drawing.Size(124, 30);

            this.fleetDonateButton.TabIndex = 4;

            this.fleetDonateButton.Text = "FLEET COMMANDER";

            this.fleetDonateButton.Click += new System.EventHandler(this.fleetDonateButton_Click);

            // 

            // openDonationPageLink

            // 

            this.openDonationPageLink.AutoSize = true;

            this.openDonationPageLink.Font = new System.Drawing.Font("Segoe UI", 8.25F);

            this.openDonationPageLink.Location = new System.Drawing.Point(19, 132);

            this.openDonationPageLink.Name = "openDonationPageLink";

            this.openDonationPageLink.Size = new System.Drawing.Size(168, 13);

            this.openDonationPageLink.TabIndex = 5;

            this.openDonationPageLink.TabStop = true;

            this.openDonationPageLink.Text = "Open full donation page with QR";

            this.openDonationPageLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.openDonationPageLink_LinkClicked);

            // 

            // actionPanel

            // 

            this.actionPanel.Controls.Add(this.uninstallLink);

            this.actionPanel.Controls.Add(this.minimizeLink);

            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Bottom;

            this.actionPanel.Location = new System.Drawing.Point(0, 592);

            this.actionPanel.Name = "actionPanel";

            this.actionPanel.Padding = new System.Windows.Forms.Padding(16, 4, 16, 2);

            this.actionPanel.Size = new System.Drawing.Size(460, 28);

            this.actionPanel.TabIndex = 2;

            // 

            // uninstallLink

            // 

            this.uninstallLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));

            this.uninstallLink.AutoSize = true;

            this.uninstallLink.Font = new System.Drawing.Font("Segoe UI", 8.25F);

            this.uninstallLink.Location = new System.Drawing.Point(392, 8);

            this.uninstallLink.Name = "uninstallLink";

            this.uninstallLink.Size = new System.Drawing.Size(52, 13);

            this.uninstallLink.TabIndex = 1;

            this.uninstallLink.TabStop = true;

            this.uninstallLink.Text = "Uninstall";

            this.uninstallLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.uninstallLink_LinkClicked);

            // 

            // minimizeLink

            // 

            this.minimizeLink.AutoSize = true;

            this.minimizeLink.Font = new System.Drawing.Font("Segoe UI", 8.25F);

            this.minimizeLink.Location = new System.Drawing.Point(19, 8);

            this.minimizeLink.Name = "minimizeLink";

            this.minimizeLink.Size = new System.Drawing.Size(132, 13);

            this.minimizeLink.TabIndex = 0;

            this.minimizeLink.TabStop = true;

            this.minimizeLink.Text = "Minimize to system tray";

            this.minimizeLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.minimizeLink_LinkClicked);

            // 

            // footerLabel

            // 

            this.footerLabel.Dock = System.Windows.Forms.DockStyle.Bottom;

            this.footerLabel.Font = new System.Drawing.Font("Segoe UI", 8f);

            this.footerLabel.Location = new System.Drawing.Point(0, 620);

            this.footerLabel.Name = "footerLabel";

            this.footerLabel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);

            this.footerLabel.Size = new System.Drawing.Size(460, 22);

            this.footerLabel.TabIndex = 3;

            this.footerLabel.Text = "Keep on trucking";

            this.footerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // 

            // MainForm

            // 

            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            this.ClientSize = new System.Drawing.Size(460, 642);

            this.Controls.Add(this.contentPanel);

            this.Controls.Add(this.actionPanel);

            this.Controls.Add(this.footerLabel);

            this.Controls.Add(this.truckDeckHeader);

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;

            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));

            this.MaximizeBox = false;

            this.Name = "MainForm";

            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            this.Text = "TruckDeck";

            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);

            this.Load += new System.EventHandler(this.MainForm_Load);

            this.Resize += new System.EventHandler(this.MainForm_Resize);

            this.contextMenuStrip.ResumeLayout(false);

            this.contentPanel.ResumeLayout(false);

            this.linksCard.ResumeLayout(false);

            this.linksCard.PerformLayout();

            this.networkCard.ResumeLayout(false);

            this.networkCard.PerformLayout();

            this.feedbackCard.ResumeLayout(false);

            this.feedbackCard.PerformLayout();

            this.statusCard.ResumeLayout(false);

            this.statusCard.PerformLayout();

            this.donationCard.ResumeLayout(false);

            this.donationCard.PerformLayout();

            this.actionPanel.ResumeLayout(false);

            this.actionPanel.PerformLayout();

            this.ResumeLayout(false);



        }



        #endregion



        private System.Windows.Forms.NotifyIcon trayIcon;

        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;

        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;

        private System.Windows.Forms.Timer statusUpdateTimer;

        private System.Windows.Forms.Label serverIpTitleLabel;

        private System.Windows.Forms.Label appUrlTitleLabel;

        private System.Windows.Forms.Label statusLabel;

        private System.Windows.Forms.Label apiEndpointUrlTitleLabel;

        private System.Windows.Forms.Label ipAddressLabel;

        private System.Windows.Forms.Label networkInterfaceTitleLabel;

        private System.Windows.Forms.ToolTip toolTip;

        private System.Windows.Forms.Timer broadcastTimer;

        private System.Windows.Forms.Timer networkRefreshTimer;

        private System.Windows.Forms.ToolStripMenuItem uninstallToolStripMenuItem;

        private Controls.TruckDeckHeader truckDeckHeader;

        private System.Windows.Forms.Panel contentPanel;

        private Controls.TruckDeckCard statusCard;

        private Controls.TruckDeckCard networkCard;

        private Controls.TruckDeckCard feedbackCard;

        private System.Windows.Forms.Label feedbackTitleLabel;

        private System.Windows.Forms.Label feedbackLedeLabel;

        private Controls.TruckDeckActionButton feedbackRateButton;

        private System.Windows.Forms.LinkLabel feedbackReviewsLink;

        private Controls.TruckDeckCard linksCard;

        private System.Windows.Forms.Label mapGeneratorTitleLabel;

        private Controls.TruckDeckActionButton mapGeneratorButton;

        private Controls.TruckDeckActionButton bridgeConfigButton;

        private System.Windows.Forms.Label bridgeConfigTitleLabel;

        private Controls.TruckDeckStatusBeacon statusBeacon;

        private System.Windows.Forms.Panel actionPanel;

        private Controls.TruckDeckActionButton openDashboardButton;

        private Controls.TruckDeckActionButton openApiButton;

        private Controls.TruckDeckCard donationCard;

        private System.Windows.Forms.Label supportTitleLabel;

        private System.Windows.Forms.Label supportLedeLabel;

        private Controls.TruckDeckActionButton fuelDonateButton;

        private Controls.TruckDeckActionButton boostDonateButton;

        private Controls.TruckDeckActionButton fleetDonateButton;

        private System.Windows.Forms.LinkLabel openDonationPageLink;

        private System.Windows.Forms.LinkLabel minimizeLink;

        private System.Windows.Forms.LinkLabel uninstallLink;

        private System.Windows.Forms.Label footerLabel;

        private System.Windows.Forms.ToolStripMenuItem showWindowToolStripMenuItem;

        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;

    }

}


