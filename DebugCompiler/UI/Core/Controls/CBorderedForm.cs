using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace DebugCompiler.UI.Core.Controls
{
    [Designer(typeof(CBorderedFormDesigner))]
    [Designer("System.Windows.Forms.Design.ParentControlDesigner, System.Design", typeof(IDesigner))]
    public partial class CBorderedForm : UserControl, IThemeableControl
    {
        // Window message constants
        private const int WM_NCHITTEST = 0x84;
        private const int WM_NCCALCSIZE = 0x83;
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

        #region Fields and Properties
        private bool _useTitleBar = true;
        private int _resizeBorderSize = 5;
        private bool _allowDesignerEditing = true;
        private DialogResult _dialogResult = DialogResult.None;

        [Category("Title Bar")]
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
        public string TitleBarTitle
        {
            get => TitleBar.TitleLabel.Text;
            set
            {
                TitleBar.TitleLabel.Text = value;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(5)]
        public int ResizeBorderSize
        {
            get => _resizeBorderSize;
            set => _resizeBorderSize = Math.Max(1, value);
        }

        [Category("Designer")]
        [DefaultValue(true)]
        public bool AllowDesignerEditing
        {
            get => _allowDesignerEditing;
            set => _allowDesignerEditing = value;
        }

        [Browsable(false)]
        public DialogResult DialogResult
        {
            get => _dialogResult;
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Editor(typeof(ParentControlDesigner), typeof(UITypeEditor))]
        public Panel ControlContents => this.DesignerContents;
        #endregion

        public CBorderedForm()
        {
            InitializeComponent();

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                EnableDesignTimeFeatures();
            }

            // Default setup
            MainPanel.Dock = DockStyle.Fill;
            DesignerContents.Dock = DockStyle.Fill;

            // Event handlers
            MouseDown += CBorderedForm_MouseDown;
            MainPanel.MouseDown += CBorderedForm_MouseDown;

            // Theme setup
            UIThemeManager.RegisterControl(this);
            UIThemeManager.ThemeChanged += OnThemeChanged_Implementation;

            // Designer attributes
            TypeDescriptor.AddAttributes(this.DesignerContents,
                new DesignerAttribute(typeof(CBFInnerPanelDesigner)));
        }

        #region Event Handlers
        private void CBorderedForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (ParentForm == null || e.Button != MouseButtons.Left) return;

            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(ParentForm.Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HT_CAPTION, 0);
        }

        private void OnThemeChanged_Implementation(UIThemeInfo theme)
        {
            if (IsDisposed || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                Invoke(new Action<UIThemeInfo>(ApplyTheme), theme);
                return;
            }
            ApplyTheme(theme);
        }
        #endregion

        #region Designer Support
        private void EnableDesignTimeFeatures()
        {
            this.SetStyle(ControlStyles.ContainerControl, true);
            this.DesignerContents.AllowDrop = true;

            if (DesignMode)
            {
                this.DesignerContents.BackColor = Color.FromArgb(30, 30, 30);
                this.DesignerContents.BorderStyle = BorderStyle.FixedSingle;
            }
        }
        #endregion

        #region Window Management
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCCALCSIZE && m.WParam.ToInt32() == 1)
            {
                return;
            }

            base.WndProc(ref m);

            if (m.Msg == WM_NCHITTEST && ParentForm?.WindowState == FormWindowState.Normal)
            {
                HandleHitTest(ref m);
            }
        }

        private void HandleHitTest(ref Message m)
        {
            Point pos = PointToClient(new Point(m.LParam.ToInt32()));

            if (pos.X <= _resizeBorderSize)
            {
                if (pos.Y <= _resizeBorderSize)
                    m.Result = (IntPtr)HTTOPLEFT;
                else if (pos.Y >= ClientSize.Height - _resizeBorderSize)
                    m.Result = (IntPtr)HTBOTTOMLEFT;
                else
                    m.Result = (IntPtr)HTLEFT;
            }
            else if (pos.X >= ClientSize.Width - _resizeBorderSize)
            {
                if (pos.Y <= _resizeBorderSize)
                    m.Result = (IntPtr)HTTOPRIGHT;
                else if (pos.Y >= ClientSize.Height - _resizeBorderSize)
                    m.Result = (IntPtr)HTBOTTOMRIGHT;
                else
                    m.Result = (IntPtr)HTRIGHT;
            }
            else if (pos.Y <= _resizeBorderSize)
            {
                m.Result = (IntPtr)HTTOP;
            }
            else if (pos.Y >= ClientSize.Height - _resizeBorderSize)
            {
                m.Result = (IntPtr)HTBOTTOM;
            }
            else if ((int)m.Result == HTCLIENT && TitleBar.Visible && pos.Y <= TitleBar.Bottom)
            {
                m.Result = (IntPtr)HTCAPTION;
            }
        }
        #endregion

        #region Public Methods
        public void SetExitButtonVisible(bool visible)
        {
            if (TitleBar != null)
            {
                TitleBar.SetExitButtonVisible(visible);
            }
        }

        public void SetDraggable(bool draggable)
        {
            if (TitleBar != null)
            {
                TitleBar.DisableDrag = !draggable;
            }
        }

        public void SetTitle(string title) => TitleBar.TitleLabel.Text = title;

        public void ApplyTheme(UIThemeInfo theme)
        {
            if (IsDisposed || !IsHandleCreated) return;

            this.BackColor = theme.BackColor;
            this.ForeColor = theme.TextColor;

            foreach (Control control in this.Controls)
            {
                if (control is IThemeableControl themeable)
                {
                    themeable.ApplyTheme(theme);
                }
            }
        }

        public IEnumerable<Control> GetThemedControls() => this.Controls.Cast<Control>();

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            UIThemeManager.RegisterControl(e.Control);
        }
        #endregion
    }

    internal static class NativeMethods
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
    }

    #region Designer Classes
    internal class CBorderedFormDesigner : ParentControlDesigner
    {
        private DesignerActionListCollection _actionLists;
        private IComponentChangeService _changeService;

        public override void Initialize(IComponent component)
        {
            base.Initialize(component);
            var form = (CBorderedForm)component;
            EnableDesignMode(form.ControlContents, "ControlContents");

            _changeService = GetService(typeof(IComponentChangeService)) as IComponentChangeService;
            if (_changeService != null)
            {
                _changeService.ComponentAdding += OnComponentAdding;
            }
        }

        public override DesignerActionListCollection ActionLists =>
            _actionLists ??= new DesignerActionListCollection { new CBorderedFormActionList(Component) };

        private void OnComponentAdding(object sender, ComponentEventArgs e)
        {
            if (e.Component is Control control && control.Parent == null)
            {
                ((CBorderedForm)Component).ControlContents.Controls.Add(control);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _changeService != null)
            {
                _changeService.ComponentAdding -= OnComponentAdding;
            }
            base.Dispose(disposing);
        }
    }

    internal class CBFInnerPanelDesigner : ParentControlDesigner
    {
        public override SelectionRules SelectionRules =>
            base.SelectionRules & ~SelectionRules.AllSizeable;

        protected override void PostFilterAttributes(IDictionary attributes)
        {
            base.PostFilterAttributes(attributes);
            attributes[typeof(DockingAttribute)] = new DockingAttribute(DockingBehavior.Never);
        }

        protected override void PostFilterProperties(IDictionary properties)
        {
            base.PostFilterProperties(properties);
            foreach (var name in new[] { "Dock", "Anchor", "Size", "Location", "Width", "Height" })
            {
                if (properties[name] is PropertyDescriptor pd)
                {
                    properties[name] = TypeDescriptor.CreateProperty(
                        Component.GetType(), pd, new BrowsableAttribute(false));
                }
            }
        }
    }

    public class CBorderedFormActionList : DesignerActionList
    {
        private readonly CBorderedForm _form;

        public CBorderedFormActionList(IComponent component) : base(component)
        {
            _form = (CBorderedForm)component;
        }

        public bool AllowDesignerEditing
        {
            get => _form.AllowDesignerEditing;
            set => _form.AllowDesignerEditing = value;
        }

        public override DesignerActionItemCollection GetSortedActionItems()
        {
            var items = new DesignerActionItemCollection();
            items.Add(new DesignerActionHeaderItem("Designer Options"));
            items.Add(new DesignerActionPropertyItem("AllowDesignerEditing",
                   "Allow Designer Editing",
                   "Designer Options"));
            return items;
        }
    }
    #endregion
}