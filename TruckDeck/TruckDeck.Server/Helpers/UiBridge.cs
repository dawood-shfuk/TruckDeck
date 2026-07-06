using System;
using System.Windows.Forms;

namespace Funbit.Ets.Telemetry.Server.Helpers
{
    /// <summary>Shows native WinForms dialogs from Web API handlers (must run on UI thread).</summary>
    public static class UiBridge
    {
        static MainForm _mainForm;

        public static void Register(MainForm form)
        {
            _mainForm = form;
        }

        public static string PickFolder(string description)
        {
            if (_mainForm == null || _mainForm.IsDisposed)
                return null;

            string result = null;
            _mainForm.Invoke(new Action(() =>
            {
                using (var dialog = new FolderBrowserDialog
                {
                    Description = description,
                    ShowNewFolderButton = false
                })
                {
                    if (dialog.ShowDialog(_mainForm) == DialogResult.OK)
                        result = dialog.SelectedPath;
                }
            }));
            return result;
        }
    }
}
