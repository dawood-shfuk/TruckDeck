using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Helpers;

namespace Funbit.Ets.Telemetry.Server.Controls
{
    public sealed class TruckDeckHeader : Panel
    {
        readonly PictureBox _iconBox = new PictureBox();
        readonly Label _titleLabel = new Label();
        readonly Label _taglineLabel = new Label();
        readonly Label _versionLabel = new Label();
        readonly Label _statusLabel = new Label();
        readonly ProgressBar _progressBar = new ProgressBar();

        string _titleText = "TRUCKDECK";
        string _taglineText = "Cabin telemetry · dashboard server";
        string _statusText = "";
        int _progressValue = -1;

        public string TitleText
        {
            get => _titleText;
            set { _titleText = value ?? ""; _titleLabel.Text = _titleText; LayoutHeader(); }
        }

        public string TaglineText
        {
            get => _taglineText;
            set { _taglineText = value ?? ""; _taglineLabel.Text = _taglineText; LayoutHeader(); }
        }

        public string VersionText
        {
            get => _versionLabel.Text;
            set => _versionLabel.Text = value ?? "";
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value ?? "";
                _statusLabel.Text = _statusText;
                LayoutHeader();
            }
        }

        /// <summary>0–100 shows the bar; -1 hides it.</summary>
        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                _progressBar.Visible = value >= 0;
                if (value >= 0)
                    _progressBar.Value = Math.Max(0, Math.Min(100, value));
                LayoutHeader();
            }
        }

        public TruckDeckHeader()
        {
            UiPaintHelper.EnableDoubleBuffer(this);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Height = 92;
            Dock = DockStyle.Top;
            BackColor = TruckDeckTheme.Bg;
            Padding = new Padding(18, 14, 18, 8);

            _iconBox.Size = new Size(44, 44);
            _iconBox.Location = new Point(18, 16);
            _iconBox.SizeMode = PictureBoxSizeMode.Zoom;
            _iconBox.BackColor = Color.Transparent;

            _titleLabel.AutoSize = true;
            _titleLabel.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
            _titleLabel.ForeColor = TruckDeckTheme.Accent;
            _titleLabel.BackColor = Color.Transparent;
            _titleLabel.Text = _titleText;

            _taglineLabel.AutoSize = true;
            _taglineLabel.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            _taglineLabel.ForeColor = TruckDeckTheme.Label;
            _taglineLabel.BackColor = Color.Transparent;
            _taglineLabel.Text = _taglineText;

            _statusLabel.AutoSize = false;
            _statusLabel.Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold);
            _statusLabel.ForeColor = TruckDeckTheme.Accent;
            _statusLabel.BackColor = Color.Transparent;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.AutoEllipsis = true;

            _progressBar.Height = 10;
            _progressBar.Visible = false;
            _progressBar.Style = ProgressBarStyle.Continuous;

            _versionLabel.AutoSize = true;
            _versionLabel.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            _versionLabel.ForeColor = TruckDeckTheme.AccentDim;
            _versionLabel.BackColor = Color.Transparent;
            _versionLabel.TextAlign = ContentAlignment.MiddleRight;

            Controls.Add(_iconBox);
            Controls.Add(_titleLabel);
            Controls.Add(_taglineLabel);
            Controls.Add(_statusLabel);
            Controls.Add(_progressBar);
            Controls.Add(_versionLabel);

            try
            {
                using (var icon = ApplicationIconHelper.Load())
                    _iconBox.Image = icon.ToBitmap();
            }
            catch
            {
                // optional icon
            }

            Resize += (_, __) => LayoutHeader();
            LayoutHeader();
        }

        void LayoutHeader()
        {
            var showStatus = _progressBar.Visible || !string.IsNullOrWhiteSpace(_statusText);
            Height = showStatus ? 118 : 92;

            _titleLabel.Location = new Point(72, 18);
            _taglineLabel.Location = new Point(74, 48);
            _versionLabel.Location = new Point(Width - _versionLabel.PreferredWidth - 18, 22);

            var barTop = 68;
            var barWidth = Math.Max(120, Width - 90);
            _progressBar.SetBounds(74, barTop, barWidth, 10);
            _statusLabel.SetBounds(74, barTop + 14, barWidth, 18);
            _statusLabel.Visible = !string.IsNullOrWhiteSpace(_statusText);

            var roadY = showStatus ? Height - 12 : Height - 14;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = ClientRectangle;
            using (var grad = new LinearGradientBrush(
                       rect,
                       Color.FromArgb(28, 32, 22),
                       TruckDeckTheme.Bg,
                       LinearGradientMode.Vertical))
            {
                g.FillRectangle(grad, rect);
            }

            var showStatus = _progressBar.Visible || !string.IsNullOrWhiteSpace(_statusText);
            var roadY = showStatus ? Height - 12 : Height - 14;
            UiPaintHelper.DrawDashedRoad(g, new Rectangle(18, roadY, Width - 36, 8), TruckDeckTheme.AccentDim);

            using (var glow = new Pen(Color.FromArgb(40, TruckDeckTheme.Accent), 1f))
                g.DrawLine(glow, 18, Height - 1, Width - 18, Height - 1);

            base.OnPaint(e);
        }
    }
}
