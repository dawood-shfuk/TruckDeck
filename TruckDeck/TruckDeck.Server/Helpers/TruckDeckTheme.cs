using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Funbit.Ets.Telemetry.Server.Controls;

namespace Funbit.Ets.Telemetry.Server.Helpers
{
    public static class TruckDeckTheme
    {
        public static readonly Color Bg = Color.FromArgb(7, 8, 10);
        public static readonly Color Panel = Color.FromArgb(19, 22, 15);
        public static readonly Color Ink = Color.FromArgb(243, 246, 236);
        public static readonly Color Label = Color.FromArgb(138, 147, 126);
        public static readonly Color Accent = Color.FromArgb(182, 255, 31);
        public static readonly Color AccentDim = Color.FromArgb(111, 138, 28);
        public static readonly Color Line = Color.FromArgb(29, 35, 22);
        public static readonly Color Connected = Color.FromArgb(57, 224, 122);
        public static readonly Color Running = Color.FromArgb(70, 182, 255);
        public static readonly Color Disconnected = Color.FromArgb(255, 90, 77);
        public static readonly Color ButtonBg = Color.FromArgb(35, 41, 31);

        public static void Apply(Form form)
        {
            ApplyModern(form);
        }

        public static void ApplyModern(Form form)
        {
            form.BackColor = Bg;
            form.ForeColor = Ink;
            form.Font = new Font("Segoe UI", 9.75f);

            if (form.MainMenuStrip != null)
                StyleMenuStrip(form.MainMenuStrip);

            ApplyControls(form.Controls);
        }

        static void ApplyControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                switch (control)
                {
                    case TruckDeckCard:
                    case TruckDeckHeader:
                    case TruckDeckStatusBeacon:
                    case TruckDeckActionButton:
                        break;
                    case GroupBox groupBox:
                        groupBox.BackColor = Panel;
                        groupBox.ForeColor = Accent;
                        groupBox.Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold);
                        break;
                    case LinkLabel link:
                        link.LinkColor = Accent;
                        link.ActiveLinkColor = Accent;
                        link.VisitedLinkColor = AccentDim;
                        if (link.Name.Contains("uninstall") || link.Name.Contains("minimize"))
                        {
                            link.LinkColor = Label;
                            link.ActiveLinkColor = Ink;
                            link.VisitedLinkColor = Label;
                            link.ForeColor = Label;
                        }
                        else
                        {
                            link.ForeColor = Accent;
                        }
                        link.BackColor = Color.Transparent;
                        break;
                    case Button button when !(button is TruckDeckActionButton):
                        button.FlatStyle = FlatStyle.Flat;
                        button.FlatAppearance.BorderColor = AccentDim;
                        button.FlatAppearance.BorderSize = 1;
                        button.BackColor = ButtonBg;
                        button.ForeColor = Accent;
                        button.Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold);
                        break;
                    case ComboBox combo:
                        combo.BackColor = ButtonBg;
                        combo.ForeColor = Ink;
                        combo.FlatStyle = FlatStyle.Flat;
                        break;
                    case Label label when !(label is LinkLabel):
                        if (label.Name == "ipAddressLabel")
                        {
                            label.ForeColor = Accent;
                        }
                        else if (label.Name == "footerLabel")
                        {
                            label.ForeColor = Label;
                        }
                        else if (label.ForeColor == SystemColors.ControlText ||
                            label.ForeColor == Color.FromArgb(64, 64, 64) ||
                            label.ForeColor == Color.Purple)
                        {
                            label.ForeColor = Label;
                        }
                        label.BackColor = Color.Transparent;
                        break;
                    case Panel panel when panel.Name == "actionPanel":
                        panel.BackColor = Color.FromArgb(12, 14, 10);
                        break;
                    case Panel panel when panel.Name == "contentPanel":
                        panel.BackColor = Bg;
                        break;
                    case MenuStrip menu:
                        StyleMenuStrip(menu);
                        break;
                    case ContextMenuStrip context:
                        StyleContextMenu(context);
                        break;
                }

                if (control.HasChildren)
                    ApplyControls(control.Controls);
            }
        }

        static void StyleContextMenu(ContextMenuStrip menu)
        {
            menu.BackColor = Panel;
            menu.ForeColor = Ink;
            menu.Renderer = new TruckDeckMenuRenderer();
            foreach (ToolStripItem item in menu.Items)
                StyleToolStripItem(item);
        }

        static void StyleMenuStrip(MenuStrip menu)
        {
            menu.BackColor = Panel;
            menu.ForeColor = Ink;
            menu.Renderer = new TruckDeckMenuRenderer();
            foreach (ToolStripItem item in menu.Items)
                StyleToolStripItem(item);
        }

        static void StyleToolStripItem(ToolStripItem item)
        {
            item.BackColor = Panel;
            item.ForeColor = Ink;
            if (item is ToolStripDropDownItem dropDown)
            {
                dropDown.DropDown.BackColor = Panel;
                dropDown.DropDown.ForeColor = Ink;
                foreach (ToolStripItem child in dropDown.DropDownItems)
                    StyleToolStripItem(child);
            }
        }

        sealed class TruckDeckMenuRenderer : ToolStripProfessionalRenderer
        {
            public TruckDeckMenuRenderer() : base(new TruckDeckColorTable()) { }
        }

        sealed class TruckDeckColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected => ButtonBg;
            public override Color MenuItemSelectedGradientBegin => ButtonBg;
            public override Color MenuItemSelectedGradientEnd => ButtonBg;
            public override Color MenuItemBorder => AccentDim;
            public override Color MenuBorder => Line;
            public override Color ToolStripDropDownBackground => Panel;
            public override Color ImageMarginGradientBegin => Panel;
            public override Color ImageMarginGradientMiddle => Panel;
            public override Color ImageMarginGradientEnd => Panel;
        }
    }
}
