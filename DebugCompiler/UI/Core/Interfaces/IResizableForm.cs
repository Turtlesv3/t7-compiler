using System.Windows.Forms;
using DebugCompiler.Properties;

namespace DebugCompiler.UI.Core.Interfaces
{
    internal interface IResizableForm
    {
        void WndProc_Implementation(ref Message m);
    }
}
