using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using System.Collections;
using DebugCompiler.UI.Core.Controls;

namespace DebugCompiler.UI.Core.Interfaces
{
    public interface IThemeableControl
    {
        void ApplyTheme(UIThemeInfo theme);
        IEnumerable<Control> GetThemedControls();
    }
}
