namespace Funbit.Ets.Telemetry.Server

{

    partial class SetupForm

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

            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupForm));

            this.okButton = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckActionButton();

            this.setupCard = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckCard();

            this.urlReservationStatusImage = new System.Windows.Forms.PictureBox();

            this.urlReservationStatusLabel = new System.Windows.Forms.Label();

            this.firewallStatusImage = new System.Windows.Forms.PictureBox();

            this.firewallStatusLabel = new System.Windows.Forms.Label();

            this.pluginStatusImage = new System.Windows.Forms.PictureBox();

            this.pluginStatusLabel = new System.Windows.Forms.Label();

            this.truckDeckHeader = new Funbit.Ets.Telemetry.Server.Controls.TruckDeckHeader();

            this.footerLabel = new System.Windows.Forms.Label();

            this.setupCard.SuspendLayout();

            ((System.ComponentModel.ISupportInitialize)(this.urlReservationStatusImage)).BeginInit();

            ((System.ComponentModel.ISupportInitialize)(this.firewallStatusImage)).BeginInit();

            ((System.ComponentModel.ISupportInitialize)(this.pluginStatusImage)).BeginInit();

            this.SuspendLayout();

            // 

            // okButton

            // 

            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));

            this.okButton.Location = new System.Drawing.Point(248, 318);

            this.okButton.Name = "okButton";

            this.okButton.Size = new System.Drawing.Size(180, 42);

            this.okButton.TabIndex = 2;

            this.okButton.Text = "Install";

            this.okButton.UseVisualStyleBackColor = true;

            this.okButton.Click += new System.EventHandler(this.okButton_Click);

            // 

            // setupCard

            // 

            this.setupCard.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 

            | System.Windows.Forms.AnchorStyles.Left) 

            | System.Windows.Forms.AnchorStyles.Right)));

            this.setupCard.CardTitle = "SETUP CHECKLIST";

            this.setupCard.Controls.Add(this.urlReservationStatusImage);

            this.setupCard.Controls.Add(this.urlReservationStatusLabel);

            this.setupCard.Controls.Add(this.firewallStatusImage);

            this.setupCard.Controls.Add(this.firewallStatusLabel);

            this.setupCard.Controls.Add(this.pluginStatusImage);

            this.setupCard.Controls.Add(this.pluginStatusLabel);

            this.setupCard.Location = new System.Drawing.Point(16, 104);

            this.setupCard.Name = "setupCard";

            this.setupCard.Size = new System.Drawing.Size(412, 200);

            this.setupCard.TabIndex = 3;

            // 

            // urlReservationStatusImage

            // 

            this.urlReservationStatusImage.Image = global::Funbit.Ets.Telemetry.Server.Properties.Resources.StatusIcon;

            this.urlReservationStatusImage.Location = new System.Drawing.Point(24, 142);

            this.urlReservationStatusImage.Name = "urlReservationStatusImage";

            this.urlReservationStatusImage.Size = new System.Drawing.Size(32, 32);

            this.urlReservationStatusImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;

            this.urlReservationStatusImage.TabIndex = 7;

            this.urlReservationStatusImage.TabStop = false;

            // 

            // urlReservationStatusLabel

            // 

            this.urlReservationStatusLabel.AutoSize = true;

            this.urlReservationStatusLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F);

            this.urlReservationStatusLabel.Location = new System.Drawing.Point(68, 149);

            this.urlReservationStatusLabel.Name = "urlReservationStatusLabel";

            this.urlReservationStatusLabel.Size = new System.Drawing.Size(99, 17);

            this.urlReservationStatusLabel.TabIndex = 6;

            this.urlReservationStatusLabel.Text = "ACL rule for URL";

            // 

            // firewallStatusImage

            // 

            this.firewallStatusImage.Image = global::Funbit.Ets.Telemetry.Server.Properties.Resources.StatusIcon;

            this.firewallStatusImage.Location = new System.Drawing.Point(24, 96);

            this.firewallStatusImage.Name = "firewallStatusImage";

            this.firewallStatusImage.Size = new System.Drawing.Size(32, 32);

            this.firewallStatusImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;

            this.firewallStatusImage.TabIndex = 5;

            this.firewallStatusImage.TabStop = false;

            // 

            // firewallStatusLabel

            // 

            this.firewallStatusLabel.AutoSize = true;

            this.firewallStatusLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F);

            this.firewallStatusLabel.Location = new System.Drawing.Point(68, 103);

            this.firewallStatusLabel.Name = "firewallStatusLabel";

            this.firewallStatusLabel.Size = new System.Drawing.Size(74, 17);

            this.firewallStatusLabel.TabIndex = 4;

            this.firewallStatusLabel.Text = "Firewall rule";

            // 

            // pluginStatusImage

            // 

            this.pluginStatusImage.Image = global::Funbit.Ets.Telemetry.Server.Properties.Resources.StatusIcon;

            this.pluginStatusImage.Location = new System.Drawing.Point(24, 50);

            this.pluginStatusImage.Name = "pluginStatusImage";

            this.pluginStatusImage.Size = new System.Drawing.Size(32, 32);

            this.pluginStatusImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;

            this.pluginStatusImage.TabIndex = 3;

            this.pluginStatusImage.TabStop = false;

            // 

            // pluginStatusLabel

            // 

            this.pluginStatusLabel.AutoSize = true;

            this.pluginStatusLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F);

            this.pluginStatusLabel.Location = new System.Drawing.Point(68, 57);

            this.pluginStatusLabel.Name = "pluginStatusLabel";

            this.pluginStatusLabel.Size = new System.Drawing.Size(156, 17);

            this.pluginStatusLabel.TabIndex = 2;

            this.pluginStatusLabel.Text = "ETS2/ATS telemetry plugin";

            // 

            // truckDeckHeader

            // 

            this.truckDeckHeader.Dock = System.Windows.Forms.DockStyle.Top;

            this.truckDeckHeader.Location = new System.Drawing.Point(0, 0);

            this.truckDeckHeader.Name = "truckDeckHeader";

            this.truckDeckHeader.Size = new System.Drawing.Size(444, 92);

            this.truckDeckHeader.TabIndex = 4;

            // 

            // footerLabel

            // 

            this.footerLabel.Dock = System.Windows.Forms.DockStyle.Bottom;

            this.footerLabel.Font = new System.Drawing.Font("Segoe UI", 8f);

            this.footerLabel.Location = new System.Drawing.Point(0, 368);

            this.footerLabel.Name = "footerLabel";

            this.footerLabel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);

            this.footerLabel.Size = new System.Drawing.Size(444, 22);

            this.footerLabel.TabIndex = 5;

            this.footerLabel.Text = "First-time setup";

            this.footerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // 

            // SetupForm

            // 

            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            this.ClientSize = new System.Drawing.Size(444, 390);

            this.Controls.Add(this.setupCard);

            this.Controls.Add(this.okButton);

            this.Controls.Add(this.footerLabel);

            this.Controls.Add(this.truckDeckHeader);

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;

            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));

            this.MaximizeBox = false;

            this.MinimizeBox = false;

            this.Name = "SetupForm";

            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            this.Text = "TruckDeck Setup";

            this.TopMost = true;

            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SetupForm_FormClosing);

            this.Load += new System.EventHandler(this.SetupForm_Load);

            this.setupCard.ResumeLayout(false);

            this.setupCard.PerformLayout();

            ((System.ComponentModel.ISupportInitialize)(this.urlReservationStatusImage)).EndInit();

            ((System.ComponentModel.ISupportInitialize)(this.firewallStatusImage)).EndInit();

            ((System.ComponentModel.ISupportInitialize)(this.pluginStatusImage)).EndInit();

            this.ResumeLayout(false);



        }



        #endregion



        private Controls.TruckDeckActionButton okButton;

        private Controls.TruckDeckCard setupCard;

        private System.Windows.Forms.PictureBox pluginStatusImage;

        private System.Windows.Forms.Label pluginStatusLabel;

        private System.Windows.Forms.PictureBox firewallStatusImage;

        private System.Windows.Forms.Label firewallStatusLabel;

        private System.Windows.Forms.PictureBox urlReservationStatusImage;

        private System.Windows.Forms.Label urlReservationStatusLabel;

        private Controls.TruckDeckHeader truckDeckHeader;

        private System.Windows.Forms.Label footerLabel;

    }

}


