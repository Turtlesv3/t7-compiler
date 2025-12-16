using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;

namespace T7CompilerGUI.Helpers
{
    /// <summary>
    /// Helper class for loading form icons from various locations
    /// </summary>
    public static class FormIconHelper
    {
        /// <summary>
        /// Loads the application icon and sets it on the specified form
        /// </summary>
        /// <param name="form">The form to set the icon on</param>
        public static void LoadFormIcon(Form form)
        {
            if (form == null)
                return;

            Icon formIcon = null;

            // Try 1: Embedded resource
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                // Try different possible resource names
                string[] resourceNames = new[] { "T7CompilerGUI.Resources.t7gui.ico", "t7gui.ico", "Resources.t7gui.ico" };
                foreach (string resourceName in resourceNames)
                {
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            formIcon = new Icon(stream);
                            break;
                        }
                    }
                }
            }
            catch { }

            // Try 2: Resources folder relative to executable
            if (formIcon == null)
            {
                try
                {
                    string iconPath = Path.Combine(Application.StartupPath, "Resources", "t7gui.ico");
                    if (File.Exists(iconPath))
                    {
                        formIcon = new Icon(iconPath);
                    }
                }
                catch { }
            }

            // Try 3: Resources folder relative to assembly location
            if (formIcon == null)
            {
                try
                {
                    string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    if (!string.IsNullOrEmpty(assemblyLocation))
                    {
                        string iconPath = Path.Combine(assemblyLocation, "Resources", "t7gui.ico");
                        if (File.Exists(iconPath))
                        {
                            formIcon = new Icon(iconPath);
                        }
                    }
                }
                catch { }
            }

            if (formIcon != null)
            {
                form.Icon = formIcon;
            }
        }
    }
}

