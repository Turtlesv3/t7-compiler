using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Refract.UI.Core.Interfaces
{
    /// <summary>
    /// Interface for controls that support dynamic theming
    /// </summary>
    internal interface IThemeableControl
    {
        /// <summary>
        /// Gets all child controls that should be registered with the theme manager
        /// </summary>
        /// <returns>Enumerable collection of controls to theme</returns>
        /// <remarks>
        /// The implementing control itself is automatically registered and doesn't need to be included
        /// </remarks>
        IEnumerable<Control> GetThemedControls();
    }
}