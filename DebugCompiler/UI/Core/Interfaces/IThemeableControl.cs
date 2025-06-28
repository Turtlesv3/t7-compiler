using DebugCompiler.UI.Core.Singletons;
using System.Collections.Generic;
using System.Windows.Forms;
using DebugCompiler.Properties;

namespace DebugCompiler.UI.Core.Interfaces
{
    public interface IThemeableControl
    {
        void ApplyTheme(UIThemeInfo theme);
        IEnumerable<Control> GetThemedControls();

    }

    public interface IThemeableContainer
    {
        IEnumerable<Control> GetThemeableChildren();
    }
}