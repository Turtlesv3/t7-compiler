using DebugCompiler.Properties;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace DebugCompiler.UI.Core.Controls
{
    [Designer(typeof(CBorderedFormDesigner))]
    public partial class CBorderedForm : UserControl, IThemeableControl
    {
        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 1;
        private const int HTCAPTION = 2;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        private const int RESIZE_HANDLE_SIZE = 5; // Reduced from 10

        protected override void WndProc(ref Message m)
        {
            const int WM_NCCALCSIZE = 0x83;

            if (m.Msg == WM_NCCALCSIZE && m.WParam.ToInt32() == 1)
            {
                // Prevent the control from trying to calculate its own non-client area
                return;
            }

            base.WndProc(ref m);

            if (m.Msg == WM_NCHITTEST)
            {
                if (ParentForm != null && ParentForm.WindowState == FormWindowState.Normal)
                {
                    Point pos = PointToClient(new Point(m.LParam.ToInt32()));
                    int resizeBorderWidth = 5; // Adjust as needed

                    // Only handle resizing if we're at the edge of the parent form
                    if (pos.X <= resizeBorderWidth)
                    {
                        if (pos.Y <= resizeBorderWidth)
                            m.Result = (IntPtr)HTTOPLEFT;
                        else if (pos.Y >= ClientSize.Height - resizeBorderWidth)
                            m.Result = (IntPtr)HTBOTTOMLEFT;
                        else
                            m.Result = (IntPtr)HTLEFT;
                    }
                    else if (pos.X >= ClientSize.Width - resizeBorderWidth)
                    {
                        if (pos.Y <= resizeBorderWidth)
                            m.Result = (IntPtr)HTTOPRIGHT;
                        else if (pos.Y >= ClientSize.Height - resizeBorderWidth)
                            m.Result = (IntPtr)HTBOTTOMRIGHT;
                        else
                            m.Result = (IntPtr)HTRIGHT;
                    }
                    else if (pos.Y <= resizeBorderWidth)
                    {
                        m.Result = (IntPtr)HTTOP;
                    }
                    else if (pos.Y >= ClientSize.Height - resizeBorderWidth)
                    {
                        m.Result = (IntPtr)HTBOTTOM;
                    }
                    else if ((int)m.Result == HTCLIENT && TitleBar.Visible && pos.Y <= TitleBar.Bottom)
                    {
                        m.Result = (IntPtr)HTCAPTION;
                    }
                }
            }
        }

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

        // Add this to the CBorderedForm class
        private DialogResult _dialogResult = DialogResult.None;

        [
            Category("Behavior"),
            Description("Gets or sets the dialog result for the form"),
            Browsable(false)
        ]
        public DialogResult DialogResult
        {
            get { return _dialogResult; }
            set
            {
                if (_dialogResult != value)
                {
                    _dialogResult = value;
                    if (value != DialogResult.None && ParentForm != null)
                    {
                        ParentForm.DialogResult = value;
                    }
                }
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