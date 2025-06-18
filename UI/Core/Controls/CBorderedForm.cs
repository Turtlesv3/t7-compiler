using Refract.UI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Refract.UI.Core.Singletons;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using System.Collections;

namespace Refract.UI.Core.Controls
{
    /// <summary>
    /// A custom bordered form control with title bar and theming support
    /// </summary>
    [Designer(typeof(CBorderedFormDesigner))]
    public partial class CBorderedForm : Form, IThemeableControl
    {
        #region Designer Properties
        private bool _useTitleBar = true;

        [Category("Title Bar")]
        [Description("Determines if a title bar should be rendered.")]
        [Browsable(true)]
        [DefaultValue(true)]
        public bool UseTitleBar
        {
            get => _useTitleBar;
            set
            {
                _useTitleBar = value;
                TitleBar.Visible = value;
                Invalidate();
            }
        }

        [Category("Title Bar")]
        [Description("Determines the title bar's text")]
        [Browsable(true)]
        [DefaultValue("")]
        public string TitleBarTitle
        {
            get => TitleBar.TitleLabel.Text;
            set
            {
                TitleBar.TitleLabel.Text = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Panel ControlContents => this.DesignerContents;
        #endregion

        #region Win32 API
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        #endregion

        public CBorderedForm()
        {
            InitializeComponent();
            MouseDown += MouseDown_Drag;
            MainPanel.MouseDown += MouseDown_Drag;

            UIThemeManager.RegisterCustomThemeHandler(typeof(CBorderedForm), ApplyThemeCustom_Implementation);
            TypeDescriptor.AddAttributes(this.DesignerContents, new DesignerAttribute(typeof(CBFInnerPanelDesigner)));

            // Set default form properties
            FormBorderStyle = FormBorderStyle.None;
        }

        private void MouseDown_Drag(object sender, MouseEventArgs e)
        {
            if (ParentForm == null || e.Button != MouseButtons.Left)
                return;

            ReleaseCapture();
            SendMessage(ParentForm.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        private void ApplyThemeCustom_Implementation(UIThemeInfo themeData)
        {
            BackColor = themeData.AccentColor;
            MainPanel.BackColor = themeData.BackColor;
        }

        public IEnumerable<Control> GetThemedControls()
        {
            yield return TitleBar;
            yield return MainPanel;
            yield return DesignerContents;
        }

        /// <summary>
        /// Sets the visibility of the exit button in the title bar
        /// </summary>
        public void SetExitHidden(bool hidden) => TitleBar.SetExitButtonVisible(!hidden);

        /// <summary>
        /// Enables or disables dragging of the form
        /// </summary>
        public void SetDraggable(bool draggable) => TitleBar.DisableDrag = !draggable;
    }

    #region Designer Classes
    internal class CBorderedFormDesigner : ParentControlDesigner
    {
        public override void Initialize(IComponent component)
        {
            base.Initialize(component);
            if (Control is CBorderedForm form)
            {
                EnableDesignMode(form.ControlContents, "ControlContents");
            }
        }

        public override bool CanParent(Control control) => false;

        protected override void OnDragOver(DragEventArgs de) => de.Effect = DragDropEffects.None;

        protected override IComponent[] CreateToolCore(ToolboxItem tool, int x, int y, int width, int height, bool hasLocation, bool hasSize)
        {
            return null;
        }
    }

    internal class CBFInnerPanelDesigner : ParentControlDesigner
    {
        public override SelectionRules SelectionRules
        {
            get
            {
                var selectionRules = base.SelectionRules;
                selectionRules &= ~SelectionRules.AllSizeable;
                return selectionRules;
            }
        }

        protected override void PostFilterAttributes(IDictionary attributes)
        {
            base.PostFilterAttributes(attributes);
            attributes[typeof(DockingAttribute)] = new DockingAttribute(DockingBehavior.Never);
        }

        protected override void PostFilterProperties(IDictionary properties)
        {
            base.PostFilterProperties(properties);

            string[] propertiesToRemove =
            {
                "Dock", "Anchor",
                "Size", "Location", "Width", "Height",
                "MinimumSize", "MaximumSize",
                "AutoSize", "AutoSizeMode",
                "Visible", "Enabled"
            };

            foreach (var item in propertiesToRemove)
            {
                if (properties.Contains(item))
                {
                    properties[item] = TypeDescriptor.CreateProperty(
                        Component.GetType(),
                        (PropertyDescriptor)properties[item],
                        new BrowsableAttribute(false));
                }
            }
        }
    }
    #endregion
}