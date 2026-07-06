namespace Funbit.Ets.Telemetry.Server
{
    partial class MapGeneratorForm
    {
        System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _pollTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        void InitializeComponent()
        {
            this.header = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckHeader();
            this.scrollPanel = new System.Windows.Forms.Panel();
            this.generateCard = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckCard();
            this.logTextBox = new System.Windows.Forms.TextBox();
            this.generateButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();
            this.activateCheckBox = new System.Windows.Forms.CheckBox();
            this.generateAtsCheckBox = new System.Windows.Forms.CheckBox();
            this.generateEts2CheckBox = new System.Windows.Forms.CheckBox();
            this.pathsCard = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckCard();
            this.savePathsButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();
            this.detectAtsButton = new System.Windows.Forms.Button();
            this.browseAtsButton = new System.Windows.Forms.Button();
            this.atsPathTextBox = new System.Windows.Forms.TextBox();
            this.atsPathLabel = new System.Windows.Forms.Label();
            this.detectEts2Button = new System.Windows.Forms.Button();
            this.browseEts2Button = new System.Windows.Forms.Button();
            this.ets2PathTextBox = new System.Windows.Forms.TextBox();
            this.ets2PathLabel = new System.Windows.Forms.Label();
            this.toolsCard = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckCard();
            this.refreshStatusButton = new System.Windows.Forms.Button();
            this.installToolsButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();
            this.mapToolsStatusLabel = new System.Windows.Forms.Label();
            this.tippecanoeStatusLabel = new System.Windows.Forms.Label();
            this.gitStatusLabel = new System.Windows.Forms.Label();
            this.nodeStatusLabel = new System.Windows.Forms.Label();
            this.wslDistroStatusLabel = new System.Windows.Forms.Label();
            this.wslCard = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckCard();
            this.installWslButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();
            this.browseWslButton = new System.Windows.Forms.Button();
            this.wslInstallPathTextBox = new System.Windows.Forms.TextBox();
            this.wslInstallHintLabel = new System.Windows.Forms.Label();
            this.closeButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();
            this.scrollPanel.SuspendLayout();
            this.generateCard.SuspendLayout();
            this.pathsCard.SuspendLayout();
            this.toolsCard.SuspendLayout();
            this.wslCard.SuspendLayout();
            this.SuspendLayout();
            //
            // header
            //
            this.header.Dock = System.Windows.Forms.DockStyle.Top;
            this.header.Location = new System.Drawing.Point(0, 0);
            this.header.Name = "header";
            this.header.Size = new System.Drawing.Size(584, 92);
            this.header.TabIndex = 0;
            //
            // scrollPanel
            //
            this.scrollPanel.AutoScroll = true;
            this.scrollPanel.Controls.Add(this.generateCard);
            this.scrollPanel.Controls.Add(this.pathsCard);
            this.scrollPanel.Controls.Add(this.toolsCard);
            this.scrollPanel.Controls.Add(this.wslCard);
            this.scrollPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scrollPanel.Location = new System.Drawing.Point(0, 92);
            this.scrollPanel.Name = "scrollPanel";
            this.scrollPanel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.scrollPanel.Size = new System.Drawing.Size(584, 579);
            this.scrollPanel.TabIndex = 1;
            //
            // wslCard
            //
            this.wslCard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.wslCard.CardTitle = "0 · WSL setup";
            this.wslCard.Controls.Add(this.installWslButton);
            this.wslCard.Controls.Add(this.browseWslButton);
            this.wslCard.Controls.Add(this.wslInstallPathTextBox);
            this.wslCard.Controls.Add(this.wslInstallHintLabel);
            this.wslCard.Location = new System.Drawing.Point(15, 11);
            this.wslCard.Name = "wslCard";
            this.wslCard.Size = new System.Drawing.Size(538, 132);
            this.wslCard.TabIndex = 0;
            //
            // wslInstallHintLabel
            //
            this.wslInstallHintLabel.AutoSize = true;
            this.wslInstallHintLabel.Location = new System.Drawing.Point(19, 36);
            this.wslInstallHintLabel.MaximumSize = new System.Drawing.Size(500, 0);
            this.wslInstallHintLabel.Name = "wslInstallHintLabel";
            this.wslInstallHintLabel.Size = new System.Drawing.Size(496, 30);
            this.wslInstallHintLabel.TabIndex = 0;
            this.wslInstallHintLabel.Text = "Map generation uses Linux tools via WSL2. Choose a folder on a drive with free space (e.g. D:\\WSL).";
            //
            // wslInstallPathTextBox
            //
            this.wslInstallPathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.wslInstallPathTextBox.Location = new System.Drawing.Point(22, 72);
            this.wslInstallPathTextBox.Name = "wslInstallPathTextBox";
            this.wslInstallPathTextBox.Size = new System.Drawing.Size(358, 23);
            this.wslInstallPathTextBox.TabIndex = 1;
            //
            // browseWslButton
            //
            this.browseWslButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseWslButton.Location = new System.Drawing.Point(386, 71);
            this.browseWslButton.Name = "browseWslButton";
            this.browseWslButton.Size = new System.Drawing.Size(68, 25);
            this.browseWslButton.TabIndex = 2;
            this.browseWslButton.Text = "Browse…";
            this.browseWslButton.UseVisualStyleBackColor = true;
            this.browseWslButton.Click += new System.EventHandler(this.browseWslButton_Click);
            //
            // installWslButton
            //
            this.installWslButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.installWslButton.Location = new System.Drawing.Point(460, 68);
            this.installWslButton.Name = "installWslButton";
            this.installWslButton.Primary = true;
            this.installWslButton.Size = new System.Drawing.Size(58, 30);
            this.installWslButton.TabIndex = 3;
            this.installWslButton.Text = "Install";
            this.installWslButton.Click += new System.EventHandler(this.installWslButton_Click);
            //
            // toolsCard
            //
            this.toolsCard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.toolsCard.CardTitle = "1 · Map tools (WSL)";
            this.toolsCard.Controls.Add(this.refreshStatusButton);
            this.toolsCard.Controls.Add(this.installToolsButton);
            this.toolsCard.Controls.Add(this.mapToolsStatusLabel);
            this.toolsCard.Controls.Add(this.tippecanoeStatusLabel);
            this.toolsCard.Controls.Add(this.gitStatusLabel);
            this.toolsCard.Controls.Add(this.nodeStatusLabel);
            this.toolsCard.Controls.Add(this.wslDistroStatusLabel);
            this.toolsCard.Location = new System.Drawing.Point(15, 149);
            this.toolsCard.Name = "toolsCard";
            this.toolsCard.Size = new System.Drawing.Size(538, 168);
            this.toolsCard.TabIndex = 1;
            //
            // wslDistroStatusLabel
            //
            this.wslDistroStatusLabel.AutoSize = true;
            this.wslDistroStatusLabel.Location = new System.Drawing.Point(22, 40);
            this.wslDistroStatusLabel.Name = "wslDistroStatusLabel";
            this.wslDistroStatusLabel.Size = new System.Drawing.Size(88, 15);
            this.wslDistroStatusLabel.TabIndex = 0;
            this.wslDistroStatusLabel.Text = "WSL distro: …";
            //
            // nodeStatusLabel
            //
            this.nodeStatusLabel.AutoSize = true;
            this.nodeStatusLabel.Location = new System.Drawing.Point(22, 60);
            this.nodeStatusLabel.Name = "nodeStatusLabel";
            this.nodeStatusLabel.Size = new System.Drawing.Size(49, 15);
            this.nodeStatusLabel.TabIndex = 1;
            this.nodeStatusLabel.Text = "Node.js";
            //
            // gitStatusLabel
            //
            this.gitStatusLabel.AutoSize = true;
            this.gitStatusLabel.Location = new System.Drawing.Point(22, 80);
            this.gitStatusLabel.Name = "gitStatusLabel";
            this.gitStatusLabel.Size = new System.Drawing.Size(23, 15);
            this.gitStatusLabel.TabIndex = 2;
            this.gitStatusLabel.Text = "Git";
            //
            // tippecanoeStatusLabel
            //
            this.tippecanoeStatusLabel.AutoSize = true;
            this.tippecanoeStatusLabel.Location = new System.Drawing.Point(22, 100);
            this.tippecanoeStatusLabel.Name = "tippecanoeStatusLabel";
            this.tippecanoeStatusLabel.Size = new System.Drawing.Size(68, 15);
            this.tippecanoeStatusLabel.TabIndex = 3;
            this.tippecanoeStatusLabel.Text = "tippecanoe";
            //
            // mapToolsStatusLabel
            //
            this.mapToolsStatusLabel.AutoSize = true;
            this.mapToolsStatusLabel.Location = new System.Drawing.Point(22, 120);
            this.mapToolsStatusLabel.Name = "mapToolsStatusLabel";
            this.mapToolsStatusLabel.Size = new System.Drawing.Size(64, 15);
            this.mapToolsStatusLabel.TabIndex = 4;
            this.mapToolsStatusLabel.Text = "Map tools";
            //
            // installToolsButton
            //
            this.installToolsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.installToolsButton.Location = new System.Drawing.Point(322, 124);
            this.installToolsButton.Name = "installToolsButton";
            this.installToolsButton.Primary = true;
            this.installToolsButton.Size = new System.Drawing.Size(140, 30);
            this.installToolsButton.TabIndex = 5;
            this.installToolsButton.Text = "Install map tools";
            this.installToolsButton.Click += new System.EventHandler(this.installToolsButton_Click);
            //
            // refreshStatusButton
            //
            this.refreshStatusButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.refreshStatusButton.Location = new System.Drawing.Point(468, 126);
            this.refreshStatusButton.Name = "refreshStatusButton";
            this.refreshStatusButton.Size = new System.Drawing.Size(50, 27);
            this.refreshStatusButton.TabIndex = 6;
            this.refreshStatusButton.Text = "Refresh";
            this.refreshStatusButton.UseVisualStyleBackColor = true;
            this.refreshStatusButton.Click += new System.EventHandler(this.refreshStatusButton_Click);
            //
            // pathsCard
            //
            this.pathsCard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pathsCard.CardTitle = "2 · Game install folders";
            this.pathsCard.Controls.Add(this.savePathsButton);
            this.pathsCard.Controls.Add(this.detectAtsButton);
            this.pathsCard.Controls.Add(this.browseAtsButton);
            this.pathsCard.Controls.Add(this.atsPathTextBox);
            this.pathsCard.Controls.Add(this.atsPathLabel);
            this.pathsCard.Controls.Add(this.detectEts2Button);
            this.pathsCard.Controls.Add(this.browseEts2Button);
            this.pathsCard.Controls.Add(this.ets2PathTextBox);
            this.pathsCard.Controls.Add(this.ets2PathLabel);
            this.pathsCard.Location = new System.Drawing.Point(15, 323);
            this.pathsCard.Name = "pathsCard";
            this.pathsCard.Size = new System.Drawing.Size(538, 168);
            this.pathsCard.TabIndex = 2;
            //
            // ets2PathLabel
            //
            this.ets2PathLabel.AutoSize = true;
            this.ets2PathLabel.Location = new System.Drawing.Point(22, 40);
            this.ets2PathLabel.Name = "ets2PathLabel";
            this.ets2PathLabel.Size = new System.Drawing.Size(123, 15);
            this.ets2PathLabel.TabIndex = 0;
            this.ets2PathLabel.Text = "Euro Truck Simulator 2";
            //
            // ets2PathTextBox
            //
            this.ets2PathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ets2PathTextBox.Location = new System.Drawing.Point(22, 58);
            this.ets2PathTextBox.Name = "ets2PathTextBox";
            this.ets2PathTextBox.Size = new System.Drawing.Size(300, 23);
            this.ets2PathTextBox.TabIndex = 1;
            //
            // browseEts2Button
            //
            this.browseEts2Button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseEts2Button.Location = new System.Drawing.Point(328, 57);
            this.browseEts2Button.Name = "browseEts2Button";
            this.browseEts2Button.Size = new System.Drawing.Size(68, 25);
            this.browseEts2Button.TabIndex = 2;
            this.browseEts2Button.Text = "Browse…";
            this.browseEts2Button.UseVisualStyleBackColor = true;
            this.browseEts2Button.Click += new System.EventHandler(this.browseEts2Button_Click);
            //
            // detectEts2Button
            //
            this.detectEts2Button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.detectEts2Button.Location = new System.Drawing.Point(402, 57);
            this.detectEts2Button.Name = "detectEts2Button";
            this.detectEts2Button.Size = new System.Drawing.Size(116, 25);
            this.detectEts2Button.TabIndex = 3;
            this.detectEts2Button.Text = "Detect Steam";
            this.detectEts2Button.UseVisualStyleBackColor = true;
            this.detectEts2Button.Click += new System.EventHandler(this.detectEts2Button_Click);
            //
            // atsPathLabel
            //
            this.atsPathLabel.AutoSize = true;
            this.atsPathLabel.Location = new System.Drawing.Point(22, 90);
            this.atsPathLabel.Name = "atsPathLabel";
            this.atsPathLabel.Size = new System.Drawing.Size(149, 15);
            this.atsPathLabel.TabIndex = 4;
            this.atsPathLabel.Text = "American Truck Simulator";
            //
            // atsPathTextBox
            //
            this.atsPathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.atsPathTextBox.Location = new System.Drawing.Point(22, 108);
            this.atsPathTextBox.Name = "atsPathTextBox";
            this.atsPathTextBox.Size = new System.Drawing.Size(300, 23);
            this.atsPathTextBox.TabIndex = 5;
            //
            // browseAtsButton
            //
            this.browseAtsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseAtsButton.Location = new System.Drawing.Point(328, 107);
            this.browseAtsButton.Name = "browseAtsButton";
            this.browseAtsButton.Size = new System.Drawing.Size(68, 25);
            this.browseAtsButton.TabIndex = 6;
            this.browseAtsButton.Click += new System.EventHandler(this.browseAtsButton_Click);
            this.browseAtsButton.Text = "Browse…";
            this.browseAtsButton.UseVisualStyleBackColor = true;
            //
            // detectAtsButton
            //
            this.detectAtsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.detectAtsButton.Location = new System.Drawing.Point(402, 107);
            this.detectAtsButton.Name = "detectAtsButton";
            this.detectAtsButton.Size = new System.Drawing.Size(116, 25);
            this.detectAtsButton.TabIndex = 7;
            this.detectAtsButton.Text = "Detect Steam";
            this.detectAtsButton.UseVisualStyleBackColor = true;
            this.detectAtsButton.Click += new System.EventHandler(this.detectAtsButton_Click);
            //
            // savePathsButton
            //
            this.savePathsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.savePathsButton.Location = new System.Drawing.Point(378, 132);
            this.savePathsButton.Name = "savePathsButton";
            this.savePathsButton.Primary = true;
            this.savePathsButton.Size = new System.Drawing.Size(140, 30);
            this.savePathsButton.TabIndex = 8;
            this.savePathsButton.Text = "Save game paths";
            this.savePathsButton.Click += new System.EventHandler(this.savePathsButton_Click);
            //
            // generateCard
            //
            this.generateCard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.generateCard.CardTitle = "3 · Generate PMTiles";
            this.generateCard.Controls.Add(this.logTextBox);
            this.generateCard.Controls.Add(this.generateButton);
            this.generateCard.Controls.Add(this.activateCheckBox);
            this.generateCard.Controls.Add(this.generateAtsCheckBox);
            this.generateCard.Controls.Add(this.generateEts2CheckBox);
            this.generateCard.Location = new System.Drawing.Point(15, 497);
            this.generateCard.Name = "generateCard";
            this.generateCard.Size = new System.Drawing.Size(538, 200);
            this.generateCard.TabIndex = 3;
            //
            // generateEts2CheckBox
            //
            this.generateEts2CheckBox.AutoSize = true;
            this.generateEts2CheckBox.Checked = true;
            this.generateEts2CheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.generateEts2CheckBox.Location = new System.Drawing.Point(22, 40);
            this.generateEts2CheckBox.Name = "generateEts2CheckBox";
            this.generateEts2CheckBox.Size = new System.Drawing.Size(149, 19);
            this.generateEts2CheckBox.TabIndex = 0;
            this.generateEts2CheckBox.Text = "Euro Truck Simulator 2";
            this.generateEts2CheckBox.UseVisualStyleBackColor = true;
            //
            // generateAtsCheckBox
            //
            this.generateAtsCheckBox.AutoSize = true;
            this.generateAtsCheckBox.Location = new System.Drawing.Point(22, 62);
            this.generateAtsCheckBox.Name = "generateAtsCheckBox";
            this.generateAtsCheckBox.Size = new System.Drawing.Size(175, 19);
            this.generateAtsCheckBox.TabIndex = 1;
            this.generateAtsCheckBox.Text = "American Truck Simulator";
            this.generateAtsCheckBox.UseVisualStyleBackColor = true;
            //
            // activateCheckBox
            //
            this.activateCheckBox.AutoSize = true;
            this.activateCheckBox.Checked = true;
            this.activateCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.activateCheckBox.Location = new System.Drawing.Point(22, 84);
            this.activateCheckBox.Name = "activateCheckBox";
            this.activateCheckBox.Size = new System.Drawing.Size(220, 19);
            this.activateCheckBox.TabIndex = 2;
            this.activateCheckBox.Text = "Activate map in dashboard after build";
            this.activateCheckBox.UseVisualStyleBackColor = true;
            //
            // generateButton
            //
            this.generateButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.generateButton.Location = new System.Drawing.Point(378, 72);
            this.generateButton.Name = "generateButton";
            this.generateButton.Primary = true;
            this.generateButton.Size = new System.Drawing.Size(140, 30);
            this.generateButton.TabIndex = 3;
            this.generateButton.Text = "Generate maps";
            this.generateButton.Click += new System.EventHandler(this.generateButton_Click);
            //
            // logTextBox
            //
            this.logTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(22)))), ((int)(((byte)(28)))));
            this.logTextBox.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.logTextBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(182)))), ((int)(((byte)(255)))), ((int)(((byte)(31)))));
            this.logTextBox.Location = new System.Drawing.Point(22, 112);
            this.logTextBox.Multiline = true;
            this.logTextBox.Name = "logTextBox";
            this.logTextBox.ReadOnly = true;
            this.logTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.logTextBox.Size = new System.Drawing.Size(496, 72);
            this.logTextBox.TabIndex = 4;
            //
            // closeButton
            //
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point(472, 639);
            this.closeButton.Name = "closeButton";
            this.closeButton.Primary = false;
            this.closeButton.Size = new System.Drawing.Size(100, 32);
            this.closeButton.TabIndex = 2;
            this.closeButton.Text = "Close";
            //
            // MapGeneratorForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(584, 681);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.scrollPanel);
            this.Controls.Add(this.header);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MapGeneratorForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "TruckDeck · Map Generator";
            this.scrollPanel.ResumeLayout(false);
            this.generateCard.ResumeLayout(false);
            this.generateCard.PerformLayout();
            this.pathsCard.ResumeLayout(false);
            this.pathsCard.PerformLayout();
            this.toolsCard.ResumeLayout(false);
            this.toolsCard.PerformLayout();
            this.wslCard.ResumeLayout(false);
            this.wslCard.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        Controls.TruckDeckHeader header;
        System.Windows.Forms.Panel scrollPanel;
        Controls.TruckDeckCard wslCard;
        System.Windows.Forms.Label wslInstallHintLabel;
        System.Windows.Forms.TextBox wslInstallPathTextBox;
        System.Windows.Forms.Button browseWslButton;
        Controls.TruckDeckActionButton installWslButton;
        Controls.TruckDeckCard toolsCard;
        System.Windows.Forms.Label wslDistroStatusLabel;
        System.Windows.Forms.Label nodeStatusLabel;
        System.Windows.Forms.Label gitStatusLabel;
        System.Windows.Forms.Label tippecanoeStatusLabel;
        System.Windows.Forms.Label mapToolsStatusLabel;
        Controls.TruckDeckActionButton installToolsButton;
        System.Windows.Forms.Button refreshStatusButton;
        Controls.TruckDeckCard pathsCard;
        System.Windows.Forms.Label ets2PathLabel;
        System.Windows.Forms.TextBox ets2PathTextBox;
        System.Windows.Forms.Button browseEts2Button;
        System.Windows.Forms.Button detectEts2Button;
        System.Windows.Forms.Label atsPathLabel;
        System.Windows.Forms.TextBox atsPathTextBox;
        System.Windows.Forms.Button browseAtsButton;
        System.Windows.Forms.Button detectAtsButton;
        Controls.TruckDeckActionButton savePathsButton;
        Controls.TruckDeckCard generateCard;
        System.Windows.Forms.CheckBox generateEts2CheckBox;
        System.Windows.Forms.CheckBox generateAtsCheckBox;
        System.Windows.Forms.CheckBox activateCheckBox;
        Controls.TruckDeckActionButton generateButton;
        System.Windows.Forms.TextBox logTextBox;
        Controls.TruckDeckActionButton closeButton;
    }
}
