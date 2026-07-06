using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Funbit.Ets.Telemetry.Server.Helpers
{
    public static class ApplicationIconHelper
    {
        public static Icon Load()
        {
            try
            {
                var exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (exeIcon != null)
                    return (Icon)exeIcon.Clone();

                var icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
                if (File.Exists(icoPath))
                    return new Icon(icoPath);
            }
            catch
            {
                // fall through
            }
            return SystemIcons.Application;
        }

        public static void Apply(Form form, NotifyIcon trayIcon = null)
        {
            if (form == null)
                return;
            var icon = Load();
            form.Icon = (Icon)icon.Clone();
            if (trayIcon != null)
                trayIcon.Icon = (Icon)icon.Clone();
            icon.Dispose();
        }
    }
}
