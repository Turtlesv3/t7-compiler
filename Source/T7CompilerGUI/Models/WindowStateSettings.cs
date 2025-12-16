using System.Drawing;
using System.Windows.Forms;

namespace T7CompilerGUI.Models
{
    /// <summary>
    /// Model representing window state (position, size, selected tab)
    /// </summary>
    public class WindowStateSettings
    {
        public FormWindowState State { get; set; } = FormWindowState.Normal;
        public Point Location { get; set; } = new Point(100, 100);
        public Size Size { get; set; } = new Size(800, 600);
        public int SelectedTab { get; set; } = 0;
    }
}

