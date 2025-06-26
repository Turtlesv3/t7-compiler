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
    [DesignerCategory("Component")]
    [Docking(DockingBehavior.Ask)]
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

        #region Fields
        private bool _useTitleBar = true;
        private int _resizeBorderSize = 5;
        private bool _allowDesignerEditing = true;
        private DialogResult _dialogResult = DialogResult.None;
        private bool _initialized = false;
        private Panel _controlContents;
        private CTitleBar _titleBar;
        #endregion

        #region Properties
        [Category("Title Bar")]
        [DefaultValue(true)]
        public bool UseTitleBar
        {
            get => _useTitleBar;
            set
            {
                if (_useTitleBar != value)
                {
                    _useTitleBar = value;
                    if (_titleBar != null) _titleBar.Visible = value;
                    if (!DesignMode) Invalidate();
                }
            }
        }

        [Category("Title Bar")]
        [Localizable(true)]
        public string TitleBarTitle
        {
            get => _titleBar?.TitleLabel?.Text ?? string.Empty;
            set
            {
                if (_titleBar != null && _titleBar.TitleLabel != null)
                {
                    _titleBar.TitleLabel.Text = value;
                    if (!DesignMode) Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(5)]
        public int ResizeBorderSize
        {
            get => _resizeBorderSize;
            set => _resizeBorderSize = Math.Max(1, Math.Min(20, value));
        }

        [Category("Designer")]
        [DefaultValue(true)]
        public bool AllowDesignerEditing
        {
            get => _allowDesignerEditing;
            set => _allowDesignerEditing = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
        [MergableProperty(false)]
        public Panel ControlContents
        {
            get => _controlContents;
            set
            {
                if (_controlContents != value)
                {
                    if (_controlContents != null)
                    {
                        Controls.Remove(_controlContents);
                        _controlContents.Dispose();
                    }
                    _controlContents = value;
                    if (_controlContents != null)
                    {
                        Controls.Add(_controlContents);
                        _controlContents.Dock = DockStyle.Fill;
                    }
                }
            }
        }

        public CTitleBar TitleBar
        {
            get => _titleBar;
            private set
            {
                if (_titleBar != value)
                {
                    if (_titleBar != null)
                    {
                        Controls.Remove(_titleBar);
                        _titleBar.Dispose();
                    }
                    _titleBar = value;
                    if (_titleBar != null)
                    {
                        Controls.Add(_titleBar);
                        _titleBar.Dock = DockStyle.Top;
                    }
                }
            }
        }
        #endregion

        public CBorderedForm()
        {
            InitializeComponent();

            SetStyle(ControlStyles.ContainerControl, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                EnableDesignTimeFeatures();
            }
            else
            {
                if (_controlContents != null)
                {
                    _controlContents.Dock = DockStyle.Fill;
                }
                _initialized = true;
            }

            UIThemeManager.RegisterControl(this);
            UIThemeManager.ThemeChanged += OnThemeChanged;
        }

        #region Theme Handling
        private void OnThemeChanged(UIThemeInfo theme)
        {
            if (IsDisposed || !IsHandleCreated || DesignMode) return;

            if (InvokeRequired)
            {
                Invoke(new Action<UIThemeInfo>(ApplyTheme), theme);
                return;
            }
            ApplyTheme(theme);
        }

        public void ApplyTheme(UIThemeInfo theme)
        {
            if (IsDisposed || !IsHandleCreated || DesignMode) return;

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
        #endregion

        #region Designer Support
        private void EnableDesignTimeFeatures()
        {
            if (_controlContents != null)
            {
                _controlContents.AllowDrop = true;
                _controlContents.BackColor = Color.FromArgb(30, 30, 30);
                _controlContents.BorderStyle = BorderStyle.FixedSingle;
            }

            // Ensure controls can be added at design time
            var host = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (host != null)
            {
                var designer = host.GetDesigner(_controlContents);
                if (designer is ParentControlDesigner pcd)
                {
                    // Use reflection to access EnableDesignMode if needed
                    var method = typeof(ParentControlDesigner).GetMethod("EnableDesignMode",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    method?.Invoke(pcd, new object[] { _controlContents, "ControlContents" });
                }
            }
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);

            // Only register theming for runtime controls
            if (_initialized && e.Control != _controlContents)
            {
                UIThemeManager.RegisterControl(e.Control);
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
            if (!_initialized) return;

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
            else if ((int)m.Result == HTCLIENT && _titleBar != null && _titleBar.Visible && pos.Y <= _titleBar.Bottom)
            {
                m.Result = (IntPtr)HTCAPTION;
            }
        }
        #endregion

        #region Public Methods
        public void SetExitButtonVisible(bool visible)
        {
            if (_titleBar != null)
            {
                _titleBar.SetExitButtonVisible(visible);
            }
        }

        public void SetDraggable(bool draggable)
        {
            if (_titleBar != null)
            {
                _titleBar.DisableDrag = !draggable;
            }
        }

        public void SetTitle(string title) => TitleBarTitle = title;
        #endregion

        #region Designer Classes
        internal class CBorderedFormDesigner : ParentControlDesigner
        {
            private IComponentChangeService _changeService;

            public override void Initialize(IComponent component)
            {
                base.Initialize(component);
                var form = (CBorderedForm)component;

                // Use reflection to access EnableDesignMode
                var method = typeof(ParentControlDesigner).GetMethod("EnableDesignMode",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                method?.Invoke(this, new object[] { form.ControlContents, "ControlContents" });

                _changeService = GetService(typeof(IComponentChangeService)) as IComponentChangeService;

                if (_changeService != null)
                {
                    _changeService.ComponentAdding += OnComponentAdding;
                }
            }

            public override bool CanParent(Control control)
            {
                // Only allow parenting to the contents panel
                return control is not CBorderedForm;
            }

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
        #endregion
    }
}