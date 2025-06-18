using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Refract.UI.Core.Interfaces
{
    /// <summary>
    /// Interface for forms that require custom resize behavior
    /// </summary>
    internal interface IResizableForm
    {
        /// <summary>
        /// Implements custom window procedure handling for resize operations
        /// </summary>
        /// <param name="m">The Windows message to process</param>
        void WndProc_Implementation(ref Message m);
    }
}