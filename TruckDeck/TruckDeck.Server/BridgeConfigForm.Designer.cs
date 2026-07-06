namespace Funbit.Ets.Telemetry.Server
{
    partial class BridgeConfigForm
    {
        System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _captureCancel?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        void InitializeComponent()
        {
            this.header = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckHeader();
            this.footerPanel = new System.Windows.Forms.Panel();
            this.statusLabel = new System.Windows.Forms.Label();
            this.cancelButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();
            this.saveButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();
            this.tabs = new System.Windows.Forms.TabControl();
            this.joystickTab = new System.Windows.Forms.TabPage();
            this.joyHintLabel = new System.Windows.Forms.Label();
            this.joyStatusLabel = new System.Windows.Forms.Label();
            this.bridgeHealthLabel = new System.Windows.Forms.Label();
            this.deviceListBox = new System.Windows.Forms.ListBox();
            this.deviceTitleLabel = new System.Windows.Forms.Label();
            this.testJoyButton = new System.Windows.Forms.Button();
            this.captureJoyButton = new System.Windows.Forms.Button();
            this.screenCycleJoyTextBox = new System.Windows.Forms.TextBox();
            this.screenCycleTitleLabel = new System.Windows.Forms.Label();
            this.hotkeysTab = new System.Windows.Forms.TabPage();
            this.duplicateWarningLabel = new System.Windows.Forms.Label();
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.searchLabel = new System.Windows.Forms.Label();
            this.hotkeysGrid = new System.Windows.Forms.DataGridView();
            this.advancedTab = new System.Windows.Forms.TabPage();
            this.restoreDefaultsButton = new System.Windows.Forms.Button();
            this.importButton = new System.Windows.Forms.Button();
            this.exportButton = new System.Windows.Forms.Button();
            this.portNoteLabel = new System.Windows.Forms.Label();
            this.portValueLabel = new System.Windows.Forms.Label();
            this.portTitleLabel = new System.Windows.Forms.Label();
            this.tapHoldNumeric = new System.Windows.Forms.NumericUpDown();
            this.tapHoldLabel = new System.Windows.Forms.Label();
            this.footerPanel.SuspendLayout();
            this.tabs.SuspendLayout();
            this.joystickTab.SuspendLayout();
            this.hotkeysTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.hotkeysGrid)).BeginInit();
            this.advancedTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tapHoldNumeric)).BeginInit();
            this.SuspendLayout();
            //
            // header
            //
            this.header.Dock = System.Windows.Forms.DockStyle.Top;
            this.header.Location = new System.Drawing.Point(0, 0);
            this.header.Name = "header";
            this.header.Size = new System.Drawing.Size(720, 92);
            this.header.TabIndex = 0;
            //
            // footerPanel
            //
            this.footerPanel.Controls.Add(this.statusLabel);
            this.footerPanel.Controls.Add(this.cancelButton);
            this.footerPanel.Controls.Add(this.saveButton);
            this.footerPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.footerPanel.Location = new System.Drawing.Point(0, 520);
            this.footerPanel.Name = "footerPanel";
            this.footerPanel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 12);
            this.footerPanel.Size = new System.Drawing.Size(720, 56);
            this.footerPanel.TabIndex = 2;
            //
            // statusLabel
            //
            this.statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.statusLabel.Location = new System.Drawing.Point(12, 14);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(420, 28);
            this.statusLabel.TabIndex = 2;
            this.statusLabel.Text = "Ready";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cancelButton
            //
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Location = new System.Drawing.Point(596, 10);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Primary = false;
            this.cancelButton.Size = new System.Drawing.Size(112, 32);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "CANCEL";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            //
            // saveButton
            //
            this.saveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.saveButton.Location = new System.Drawing.Point(472, 10);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(112, 32);
            this.saveButton.TabIndex = 0;
            this.saveButton.Text = "SAVE";
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            //
            // tabs
            //
            this.tabs.Controls.Add(this.joystickTab);
            this.tabs.Controls.Add(this.hotkeysTab);
            this.tabs.Controls.Add(this.advancedTab);
            this.tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabs.Location = new System.Drawing.Point(0, 92);
            this.tabs.Name = "tabs";
            this.tabs.Padding = new System.Drawing.Point(12, 6);
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(720, 428);
            this.tabs.TabIndex = 1;
            //
            // joystickTab
            //
            this.joystickTab.Controls.Add(this.joyHintLabel);
            this.joystickTab.Controls.Add(this.joyStatusLabel);
            this.joystickTab.Controls.Add(this.bridgeHealthLabel);
            this.joystickTab.Controls.Add(this.deviceListBox);
            this.joystickTab.Controls.Add(this.deviceTitleLabel);
            this.joystickTab.Controls.Add(this.testJoyButton);
            this.joystickTab.Controls.Add(this.captureJoyButton);
            this.joystickTab.Controls.Add(this.screenCycleJoyTextBox);
            this.joystickTab.Controls.Add(this.screenCycleTitleLabel);
            this.joystickTab.Location = new System.Drawing.Point(4, 27);
            this.joystickTab.Name = "joystickTab";
            this.joystickTab.Padding = new System.Windows.Forms.Padding(12);
            this.joystickTab.Size = new System.Drawing.Size(712, 397);
            this.joystickTab.TabIndex = 0;
            this.joystickTab.Text = "Joystick";
            this.joystickTab.UseVisualStyleBackColor = true;
            //
            // joyHintLabel
            //
            this.joyHintLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.joyHintLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.joyHintLabel.Location = new System.Drawing.Point(15, 72);
            this.joyHintLabel.Name = "joyHintLabel";
            this.joyHintLabel.Size = new System.Drawing.Size(682, 56);
            this.joyHintLabel.TabIndex = 8;
            this.joyHintLabel.Text = "TruckDeck only — this button cycles dashboard skins on your phone/tablet. It does not steer or control the truck.\r\nSteering, pedals, and truck functions: ETS2/ATS → Options → Controls & Keys.\r\nFormat joyN.bM (e.g. joy1.b1). Disabled if no wheel is connected.";
            //
            // joyStatusLabel
            //
            this.joyStatusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.joyStatusLabel.Location = new System.Drawing.Point(15, 132);
            this.joyStatusLabel.Name = "joyStatusLabel";
            this.joyStatusLabel.Size = new System.Drawing.Size(682, 20);
            this.joyStatusLabel.TabIndex = 7;
            this.joyStatusLabel.Text = "Joy status";
            //
            // bridgeHealthLabel
            //
            this.bridgeHealthLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.bridgeHealthLabel.Location = new System.Drawing.Point(15, 156);
            this.bridgeHealthLabel.Name = "bridgeHealthLabel";
            this.bridgeHealthLabel.Size = new System.Drawing.Size(682, 20);
            this.bridgeHealthLabel.TabIndex = 6;
            this.bridgeHealthLabel.Text = "Bridge health";
            //
            // deviceListBox
            //
            this.deviceListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.deviceListBox.FormattingEnabled = true;
            this.deviceListBox.IntegralHeight = false;
            this.deviceListBox.ItemHeight = 15;
            this.deviceListBox.Location = new System.Drawing.Point(15, 204);
            this.deviceListBox.Name = "deviceListBox";
            this.deviceListBox.Size = new System.Drawing.Size(682, 179);
            this.deviceListBox.TabIndex = 5;
            //
            // deviceTitleLabel
            //
            this.deviceTitleLabel.AutoSize = true;
            this.deviceTitleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.deviceTitleLabel.Location = new System.Drawing.Point(15, 184);
            this.deviceTitleLabel.Name = "deviceTitleLabel";
            this.deviceTitleLabel.Size = new System.Drawing.Size(115, 15);
            this.deviceTitleLabel.TabIndex = 4;
            this.deviceTitleLabel.Text = "Detected devices";
            //
            // testJoyButton
            //
            this.testJoyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.testJoyButton.Location = new System.Drawing.Point(622, 36);
            this.testJoyButton.Name = "testJoyButton";
            this.testJoyButton.Size = new System.Drawing.Size(75, 27);
            this.testJoyButton.TabIndex = 3;
            this.testJoyButton.Text = "Test";
            this.testJoyButton.UseVisualStyleBackColor = true;
            this.testJoyButton.Click += new System.EventHandler(this.testJoyButton_Click);
            //
            // captureJoyButton
            //
            this.captureJoyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.captureJoyButton.Location = new System.Drawing.Point(531, 36);
            this.captureJoyButton.Name = "captureJoyButton";
            this.captureJoyButton.Size = new System.Drawing.Size(85, 27);
            this.captureJoyButton.TabIndex = 2;
            this.captureJoyButton.Text = "Capture";
            this.captureJoyButton.UseVisualStyleBackColor = true;
            this.captureJoyButton.Click += new System.EventHandler(this.captureJoyButton_Click);
            //
            // screenCycleJoyTextBox
            //
            this.screenCycleJoyTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.screenCycleJoyTextBox.Location = new System.Drawing.Point(15, 38);
            this.screenCycleJoyTextBox.Name = "screenCycleJoyTextBox";
            this.screenCycleJoyTextBox.Size = new System.Drawing.Size(510, 23);
            this.screenCycleJoyTextBox.TabIndex = 1;
            this.screenCycleJoyTextBox.TextChanged += new System.EventHandler(this.OnFieldChanged);
            //
            // screenCycleTitleLabel
            //
            this.screenCycleTitleLabel.AutoSize = true;
            this.screenCycleTitleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.screenCycleTitleLabel.Location = new System.Drawing.Point(15, 16);
            this.screenCycleTitleLabel.Name = "screenCycleTitleLabel";
            this.screenCycleTitleLabel.Size = new System.Drawing.Size(188, 15);
            this.screenCycleTitleLabel.TabIndex = 0;
            this.screenCycleTitleLabel.Text = "Dashboard screen-cycle (TruckDeck only)";
            //
            // hotkeysTab
            //
            this.hotkeysTab.Controls.Add(this.duplicateWarningLabel);
            this.hotkeysTab.Controls.Add(this.searchTextBox);
            this.hotkeysTab.Controls.Add(this.searchLabel);
            this.hotkeysTab.Controls.Add(this.hotkeysGrid);
            this.hotkeysTab.Location = new System.Drawing.Point(4, 27);
            this.hotkeysTab.Name = "hotkeysTab";
            this.hotkeysTab.Padding = new System.Windows.Forms.Padding(12);
            this.hotkeysTab.Size = new System.Drawing.Size(712, 397);
            this.hotkeysTab.TabIndex = 1;
            this.hotkeysTab.Text = "Hotkeys";
            this.hotkeysTab.UseVisualStyleBackColor = true;
            //
            // duplicateWarningLabel
            //
            this.duplicateWarningLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.duplicateWarningLabel.ForeColor = System.Drawing.Color.DarkGoldenrod;
            this.duplicateWarningLabel.Location = new System.Drawing.Point(15, 44);
            this.duplicateWarningLabel.Name = "duplicateWarningLabel";
            this.duplicateWarningLabel.Size = new System.Drawing.Size(682, 18);
            this.duplicateWarningLabel.TabIndex = 3;
            //
            // searchTextBox
            //
            this.searchTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.searchTextBox.Location = new System.Drawing.Point(64, 12);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(633, 23);
            this.searchTextBox.TabIndex = 2;
            this.searchTextBox.TextChanged += new System.EventHandler(this.searchTextBox_TextChanged);
            //
            // searchLabel
            //
            this.searchLabel.AutoSize = true;
            this.searchLabel.Location = new System.Drawing.Point(15, 15);
            this.searchLabel.Name = "searchLabel";
            this.searchLabel.Size = new System.Drawing.Size(45, 15);
            this.searchLabel.TabIndex = 1;
            this.searchLabel.Text = "Search";
            //
            // hotkeysGrid
            //
            this.hotkeysGrid.AllowUserToAddRows = false;
            this.hotkeysGrid.AllowUserToDeleteRows = false;
            this.hotkeysGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.hotkeysGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.hotkeysGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.hotkeysGrid.Location = new System.Drawing.Point(15, 68);
            this.hotkeysGrid.MultiSelect = false;
            this.hotkeysGrid.Name = "hotkeysGrid";
            this.hotkeysGrid.RowHeadersVisible = false;
            this.hotkeysGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.hotkeysGrid.Size = new System.Drawing.Size(682, 316);
            this.hotkeysGrid.TabIndex = 0;
            this.hotkeysGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.hotkeysGrid_CellContentClick);
            this.hotkeysGrid.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.hotkeysGrid_CellValueChanged);
            this.hotkeysGrid.CurrentCellDirtyStateChanged += new System.EventHandler(this.hotkeysGrid_CurrentCellDirtyStateChanged);
            //
            // advancedTab
            //
            this.advancedTab.Controls.Add(this.restoreDefaultsButton);
            this.advancedTab.Controls.Add(this.importButton);
            this.advancedTab.Controls.Add(this.exportButton);
            this.advancedTab.Controls.Add(this.portNoteLabel);
            this.advancedTab.Controls.Add(this.portValueLabel);
            this.advancedTab.Controls.Add(this.portTitleLabel);
            this.advancedTab.Controls.Add(this.tapHoldNumeric);
            this.advancedTab.Controls.Add(this.tapHoldLabel);
            this.advancedTab.Location = new System.Drawing.Point(4, 27);
            this.advancedTab.Name = "advancedTab";
            this.advancedTab.Padding = new System.Windows.Forms.Padding(12);
            this.advancedTab.Size = new System.Drawing.Size(712, 397);
            this.advancedTab.TabIndex = 2;
            this.advancedTab.Text = "Advanced";
            this.advancedTab.UseVisualStyleBackColor = true;
            //
            // restoreDefaultsButton
            //
            this.restoreDefaultsButton.Location = new System.Drawing.Point(15, 164);
            this.restoreDefaultsButton.Name = "restoreDefaultsButton";
            this.restoreDefaultsButton.Size = new System.Drawing.Size(160, 30);
            this.restoreDefaultsButton.TabIndex = 7;
            this.restoreDefaultsButton.Text = "Restore all defaults";
            this.restoreDefaultsButton.UseVisualStyleBackColor = true;
            this.restoreDefaultsButton.Click += new System.EventHandler(this.restoreDefaultsButton_Click);
            //
            // importButton
            //
            this.importButton.Location = new System.Drawing.Point(181, 128);
            this.importButton.Name = "importButton";
            this.importButton.Size = new System.Drawing.Size(160, 30);
            this.importButton.TabIndex = 6;
            this.importButton.Text = "Import JSON…";
            this.importButton.UseVisualStyleBackColor = true;
            this.importButton.Click += new System.EventHandler(this.importButton_Click);
            //
            // exportButton
            //
            this.exportButton.Location = new System.Drawing.Point(15, 128);
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new System.Drawing.Size(160, 30);
            this.exportButton.TabIndex = 5;
            this.exportButton.Text = "Export JSON…";
            this.exportButton.UseVisualStyleBackColor = true;
            this.exportButton.Click += new System.EventHandler(this.exportButton_Click);
            //
            // portNoteLabel
            //
            this.portNoteLabel.AutoSize = true;
            this.portNoteLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.portNoteLabel.Location = new System.Drawing.Point(15, 96);
            this.portNoteLabel.Name = "portNoteLabel";
            this.portNoteLabel.Size = new System.Drawing.Size(286, 15);
            this.portNoteLabel.TabIndex = 4;
            this.portNoteLabel.Text = "Change App.config InputBridgePort and restart TruckDeck.";
            //
            // portValueLabel
            //
            this.portValueLabel.AutoSize = true;
            this.portValueLabel.Location = new System.Drawing.Point(120, 72);
            this.portValueLabel.Name = "portValueLabel";
            this.portValueLabel.Size = new System.Drawing.Size(35, 15);
            this.portValueLabel.TabIndex = 3;
            this.portValueLabel.Text = "25556";
            //
            // portTitleLabel
            //
            this.portTitleLabel.AutoSize = true;
            this.portTitleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.portTitleLabel.Location = new System.Drawing.Point(15, 72);
            this.portTitleLabel.Name = "portTitleLabel";
            this.portTitleLabel.Size = new System.Drawing.Size(74, 15);
            this.portTitleLabel.TabIndex = 2;
            this.portTitleLabel.Text = "Bridge port";
            //
            // tapHoldNumeric
            //
            this.tapHoldNumeric.Location = new System.Drawing.Point(15, 36);
            this.tapHoldNumeric.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            this.tapHoldNumeric.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            this.tapHoldNumeric.Name = "tapHoldNumeric";
            this.tapHoldNumeric.Size = new System.Drawing.Size(80, 23);
            this.tapHoldNumeric.TabIndex = 1;
            this.tapHoldNumeric.Value = new decimal(new int[] { 60, 0, 0, 0 });
            this.tapHoldNumeric.ValueChanged += new System.EventHandler(this.OnFieldChanged);
            //
            // tapHoldLabel
            //
            this.tapHoldLabel.AutoSize = true;
            this.tapHoldLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.tapHoldLabel.Location = new System.Drawing.Point(15, 16);
            this.tapHoldLabel.Name = "tapHoldLabel";
            this.tapHoldLabel.Size = new System.Drawing.Size(133, 15);
            this.tapHoldLabel.TabIndex = 0;
            this.tapHoldLabel.Text = "Key tap hold (ms)";
            //
            // BridgeConfigForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(720, 576);
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.footerPanel);
            this.Controls.Add(this.header);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(640, 520);
            this.Name = "BridgeConfigForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Bridge Config";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BridgeConfigForm_FormClosing);
            this.footerPanel.ResumeLayout(false);
            this.tabs.ResumeLayout(false);
            this.joystickTab.ResumeLayout(false);
            this.joystickTab.PerformLayout();
            this.hotkeysTab.ResumeLayout(false);
            this.hotkeysTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.hotkeysGrid)).EndInit();
            this.advancedTab.ResumeLayout(false);
            this.advancedTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tapHoldNumeric)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        Controls.TruckDeckHeader header;
        System.Windows.Forms.Panel footerPanel;
        System.Windows.Forms.Label statusLabel;
        Controls.TruckDeckActionButton cancelButton;
        Controls.TruckDeckActionButton saveButton;
        System.Windows.Forms.TabControl tabs;
        System.Windows.Forms.TabPage joystickTab;
        System.Windows.Forms.TabPage hotkeysTab;
        System.Windows.Forms.TabPage advancedTab;
        System.Windows.Forms.Label screenCycleTitleLabel;
        System.Windows.Forms.TextBox screenCycleJoyTextBox;
        System.Windows.Forms.Button captureJoyButton;
        System.Windows.Forms.Button testJoyButton;
        System.Windows.Forms.Label deviceTitleLabel;
        System.Windows.Forms.ListBox deviceListBox;
        System.Windows.Forms.Label bridgeHealthLabel;
        System.Windows.Forms.Label joyStatusLabel;
        System.Windows.Forms.Label joyHintLabel;
        System.Windows.Forms.DataGridView hotkeysGrid;
        System.Windows.Forms.Label searchLabel;
        System.Windows.Forms.TextBox searchTextBox;
        System.Windows.Forms.Label duplicateWarningLabel;
        System.Windows.Forms.Label tapHoldLabel;
        System.Windows.Forms.NumericUpDown tapHoldNumeric;
        System.Windows.Forms.Label portTitleLabel;
        System.Windows.Forms.Label portValueLabel;
        System.Windows.Forms.Label portNoteLabel;
        System.Windows.Forms.Button exportButton;
        System.Windows.Forms.Button importButton;
        System.Windows.Forms.Button restoreDefaultsButton;
    }
}
