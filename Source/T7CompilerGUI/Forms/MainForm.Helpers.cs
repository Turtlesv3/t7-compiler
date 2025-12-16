using System;
using System.Windows.Forms;
using ReaLTaiizor.Controls;

namespace T7CompilerGUI.Forms
{
    public partial class MainForm
    {
        /// <summary>
        /// Updates the progress bar value and refreshes the UI
        /// </summary>
        private void UpdateProgress(int value)
        {
            if (progressBar != null)
            {
                progressBar.Value = Math.Max(0, Math.Min(100, value));
                progressBar.Invalidate();
                Application.DoEvents();
            }
        }

        /// <summary>
        /// Scrolls the log textbox to the end
        /// </summary>
        private void ScrollLogToEnd()
        {
            if (txtLog != null)
            {
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
        }
    }
}
