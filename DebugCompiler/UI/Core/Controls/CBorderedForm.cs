using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using System.Collections;
using DebugCompiler.UI.Core.Singletons;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Controls;

//http://www.reza-aghaei.com/enable-designer-of-child-panel-in-a-usercontrol/
//TODO: https://stackoverflow.com/questions/2575216/how-to-move-and-resize-a-form-without-a-border
namespace DebugCompiler.UI.Core.Controls
{
    [Designer(typeof(CBorderedFormDesigner))]
    public partial class CBorderedForm : UserControl, IThemeableControl
    {

        #region designer
        private bool __useTitleBar = true;
        [
            Category("Title Bar"),
            Description("Determines if a title bar should be rendered."),
            Browsable(true)
        ]
        public bool UseTitleBar
        {
            get
            {
                return __useTitleBar;
            }
            set
            {
                __useTitleBar = value;
                TitleBar.Visible = value;
                Invalidate();
            }
        }
        [
            Category("Title Bar"),
            Description("Determines the title bar's text"),
            Browsable(true)
        ]
        public string TitleBarTitle
        {
            get
            {
                return TitleBar.TitleLabel.Text;
            }
            set
            {
                TitleBar.TitleLabel.Text = value;
                Invalidate();
            }
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Panel ControlContents
        {
            get { return this.DesignerContents; }
        }
        #endregion

        public void ApplyTheme(UIThemeInfo theme)
        {
            this.BackColor = theme.BackColor;
            this.ForeColor = theme.TextColor;

            // Apply theme to all child controls
            foreach (Control control in this.Controls)
            {
                if (control is IThemeableControl themeable)
                {
                    themeable.ApplyTheme(theme);
                }
            }
        }

        public CBorderedForm()
        {
            InitializeComponent();
            MouseDown += MouseDown_Drag;
            MainPanel.MouseDown += MouseDown_Drag;
            UIThemeManager.RegisterControl(this);
            UIThemeManager.ThemeChanged += OnThemeChanged_Implementation;
            TypeDescriptor.AddAttributes(this.DesignerContents,
            new DesignerAttribute(typeof(CBFInnerPanelDesigner)));
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void MouseDown_Drag(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (ParentForm == null) return;
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(ParentForm.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void OnThemeChanged_Implementation(UIThemeInfo theme)
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
                return;

            if (InvokeRequired)
            {
                if (IsHandleCreated)
                {
                    Invoke(new Action<UIThemeInfo>(OnThemeChanged_Implementation), theme);
                }
                return;
            }

            ApplyTheme(theme);
        }

        public static void SafeInvoke(Control control, Action action)
        {
            if (control != null && !control.IsDisposed && control.IsHandleCreated)
            {
                if (control.InvokeRequired)
                    control.Invoke(action);
                else
                    action();
            }
        }

        public IEnumerable<Control> GetThemedControls()
        {
            return this.Controls.Cast<Control>();
        }

        public void SetExitHidden(bool hidden)
        {
            TitleBar.SetExitButtonVisible(!hidden);
        }

        public void SetDraggable(bool draggable)
        {
            TitleBar.DisableDrag = !draggable;
        }
    }

    #region designer
    internal class CBorderedFormDesigner : ParentControlDesigner
    {
        public override void Initialize(IComponent component)
        {
            base.Initialize(component);
            var contentsPanel = ((CBorderedForm)this.Control).ControlContents;
            EnableDesignMode(contentsPanel, "ControlContents");
        }
        public override bool CanParent(Control control)
        {
            return false;
        }
        protected override void OnDragOver(DragEventArgs de)
        {
            de.Effect = DragDropEffects.None;
        }
        protected override IComponent[] CreateToolCore(ToolboxItem tool,
            int x, int y, int width, int height, bool hasLocation, bool hasSize)
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
                SelectionRules selectionRules = base.SelectionRules;
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
            var propertiesToRemove = new string[] {
            "Dock", "Anchor",
            "Size", "Location", "Width", "Height",
            "MinimumSize", "MaximumSize",
            "AutoSize", "AutoSizeMode",
            "Visible", "Enabled",
        };
            foreach (var item in propertiesToRemove)
            {
                if (properties.Contains(item))
                    properties[item] = TypeDescriptor.CreateProperty(this.Component.GetType(),
                        (PropertyDescriptor)properties[item],
                        new BrowsableAttribute(false));
            }
        }
    }
    #endregion
}
