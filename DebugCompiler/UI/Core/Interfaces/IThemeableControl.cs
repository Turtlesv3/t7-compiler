using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Refract.UI.Core.Interfaces
{
    public interface IThemeableControl
    {
        IEnumerable<Control> GetThemedControls();
    }
}