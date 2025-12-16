using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Indentation.CSharp;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Drawing;
using T7CompilerGUI.Helpers; // For PoisonWpfHelper
using ReaLTaiizor.Drawing.Poison;
using ScrollViewer = System.Windows.Controls.ScrollViewer;
using Border = System.Windows.Controls.Border;
using Grid = System.Windows.Controls.Grid;
using Rectangle = System.Windows.Shapes.Rectangle;
using ScrollBar = System.Windows.Controls.Primitives.ScrollBar;
using Track = System.Windows.Controls.Primitives.Track;

namespace T7CompilerGUI.Controls
{
    /// <summary>
    /// Wraps AvalonEdit TextEditor in an ElementHost for use in WinForms
    /// Provides a similar interface to ScintillaNET for easier migration
    /// </summary>
    public partial class AvalonEditWrapper : System.Windows.Forms.UserControl
    {
        private TextMarkerService textMarkerService;
        private FoldingManager foldingManager;
        private BraceFoldingStrategy foldingStrategy;
        private ReaLTaiizor.Manager.PoisonStyleManager styleManager;

        // Scrollbar customization properties - allow external control over scrollbar appearance
        /// <summary>
        /// Gets or sets whether to use Poison theme colors for scrollbar thumb
        /// When true, uses theme accent color; when false, uses custom ScrollbarThumbColor
        /// </summary>
        public bool UsePoisonScrollbarTheme { get; set; } = true;
        
        /// <summary>
        /// Gets or sets custom scrollbar thumb color (only used if UsePoisonScrollbarTheme is false)
        /// </summary>
        public System.Drawing.Color? ScrollbarThumbColor { get; set; } = null;
        
        /// <summary>
        /// Gets or sets scrollbar thumb opacity (0.0 to 1.0)
        /// </summary>
        public double ScrollbarThumbOpacity { get; set; } = 1.0;

        public TextEditor Editor => textEditor;
        
        /// <summary>
        /// Gets or sets the Poison StyleManager for theming
        /// </summary>
        public ReaLTaiizor.Manager.PoisonStyleManager StyleManager
        {
            get => styleManager;
            set
            {
                styleManager = value;
                ApplyPoisonTheme();
            }
        }

        public new string Text
        {
            get => textEditor?.Text ?? "";
            set
            {
                if (textEditor != null)
                {
                    textEditor.Text = value ?? "";
                }
            }
        }

        public int TextLength => textEditor?.Document?.TextLength ?? 0;

        public int CurrentLine
        {
            get => textEditor?.Document?.GetLineByNumber(textEditor.TextArea.Caret.Line)?.LineNumber ?? 1;
            set
            {
                if (textEditor != null && value > 0 && value <= textEditor.Document.LineCount)
                {
                    var line = textEditor.Document.GetLineByNumber(value);
                    textEditor.TextArea.Caret.Line = line.LineNumber;
                    textEditor.ScrollToLine(line.LineNumber);
                }
            }
        }

        public int CurrentColumn
        {
            get => textEditor?.TextArea?.Caret?.Column ?? 1;
            set
            {
                if (textEditor != null && value > 0)
                {
                    textEditor.TextArea.Caret.Column = value;
                }
            }
        }

        public int SelectionStart
        {
            get
            {
                if (textEditor?.TextArea?.Selection == null)
                    return 0;
                return textEditor.Document.GetOffset(textEditor.TextArea.Selection.StartPosition.Line, textEditor.TextArea.Selection.StartPosition.Column);
            }
            set
            {
                if (textEditor?.TextArea != null && value >= 0 && value < textEditor.Document.TextLength)
                {
                    var location = textEditor.Document.GetLocation(value);
                    textEditor.TextArea.Caret.Line = location.Line;
                    textEditor.TextArea.Caret.Column = location.Column;
                    textEditor.TextArea.ClearSelection();
                }
            }
        }

        public int SelectionLength
        {
            get
            {
                if (textEditor?.TextArea?.Selection == null || textEditor.TextArea.Selection.IsEmpty)
                    return 0;
                return textEditor.Document.GetOffset(textEditor.TextArea.Selection.EndPosition.Line, textEditor.TextArea.Selection.EndPosition.Column) -
                       textEditor.Document.GetOffset(textEditor.TextArea.Selection.StartPosition.Line, textEditor.TextArea.Selection.StartPosition.Column);
            }
            set
            {
                if (textEditor?.TextArea != null && value > 0)
                {
                    int start = SelectionStart;
                    int end = Math.Min(start + value, textEditor.Document.TextLength);
                    var startLoc = textEditor.Document.GetLocation(start);
                    var endLoc = textEditor.Document.GetLocation(end);
                    
                    // Set selection using TextEditor's SelectionStart and SelectionLength
                    int startOffset = textEditor.Document.GetOffset(startLoc);
                    int endOffset = textEditor.Document.GetOffset(endLoc);
                    textEditor.SelectionStart = startOffset;
                    textEditor.SelectionLength = endOffset - startOffset;
                    textEditor.TextArea.Caret.Line = endLoc.Line;
                    textEditor.TextArea.Caret.Column = endLoc.Column;
                }
            }
        }

        public string SelectedText
        {
            get => textEditor?.TextArea?.Selection?.GetText() ?? "";
            set
            {
                if (textEditor?.TextArea?.Selection != null && !textEditor.TextArea.Selection.IsEmpty)
                {
                    textEditor.Document.Replace(
                        textEditor.Document.GetOffset(textEditor.TextArea.Selection.StartPosition.Line, textEditor.TextArea.Selection.StartPosition.Column),
                        SelectionLength,
                        value ?? ""
                    );
                }
            }
        }

        public bool ReadOnly
        {
            get => textEditor?.IsReadOnly ?? false;
            set
            {
                if (textEditor != null)
                {
                    textEditor.IsReadOnly = value;
                }
            }
        }

        public bool WordWrap
        {
            get => textEditor?.WordWrap ?? false;
            set
            {
                if (textEditor != null)
                {
                    textEditor.WordWrap = value;
                }
            }
        }

        public new event EventHandler TextChanged;
        public new event System.Windows.Forms.KeyEventHandler KeyDown;
        // KeyPress event not used - removed to avoid warning
        // public new event System.Windows.Forms.KeyPressEventHandler KeyPress;
        public new event System.Windows.Forms.MouseEventHandler MouseClick;
        public new event System.Windows.Forms.MouseEventHandler MouseDoubleClick;

        public AvalonEditWrapper()
        {
            InitializeComponent();
            SetupEditor();
        }

        private void SetupEditor()
        {
            // Enable line numbers (line number margin)
            textEditor.ShowLineNumbers = true;
            
            // Configure scrollbar visibility for invisible thumb-only style
            // Use Auto so scrollbars appear when needed, but we'll style them to be invisible except for thumb
            textEditor.HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
            
            // Vertical scrollbar should be Auto - we'll make it invisible except for thumb via styling
            textEditor.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
            
            // Set font to Consolas (monospace) - standard for code editors
            // This matches the original behavior and provides better code readability
            textEditor.FontFamily = new System.Windows.Media.FontFamily("Consolas");
            
            // Ensure theme is applied when editor is fully loaded
            // This is critical for WPF elements that load asynchronously
            textEditor.Loaded += (s, e) =>
            {
                if (styleManager != null)
                {
                    // Re-apply theme when editor is loaded to ensure all WPF elements are themed
                    // Use Background priority to avoid blocking UI updates
                    textEditor.Dispatcher?.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() => ApplyPoisonTheme()));
                }
            };
            
            // Also apply theme when TextArea is loaded (more specific timing for margins)
            // Use low priority to avoid blocking UI updates
            textEditor.TextArea.Loaded += (s, e) =>
            {
                if (styleManager != null)
                {
                    textEditor.Dispatcher?.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() =>
                        {
                            UpdateLineNumberMarginColors();
                            UpdateScrollbarColors();
                            // Force scrollbar refresh to ensure colors are applied
                            RefreshScrollbar();
                        }));
                }
            };
            
            // Also update scrollbar when layout changes (ensures it stays themed)
            // Use PoisonWpfHelper for dispatcher operations
            textEditor.LayoutUpdated += (s, e) =>
            {
                if (styleManager != null)
                {
                    // Use PoisonWpfHelper for dispatcher operations (cleaner API)
                    PoisonWpfHelper.InvokeOnDispatcher(textEditor, 
                        () => UpdateScrollbarColors(), 
                        System.Windows.Threading.DispatcherPriority.Background);
                }
            };
            
            // Enable mouse auto-scroll (middle mouse button drag scrolling)
            textEditor.TextArea.MouseWheel += (s, e) =>
            {
                if (e.Delta != 0)
                {
                    textEditor.ScrollToVerticalOffset(textEditor.VerticalOffset - (e.Delta / 3.0));
                    e.Handled = true;
                }
            };
            
            // Enable middle mouse button auto-scroll
            bool isMiddleMouseScrolling = false;
            System.Windows.Point scrollStartPoint = new System.Windows.Point();
            double scrollStartOffset = 0;
            
            textEditor.TextArea.MouseDown += (s, e) =>
            {
                if (e.MiddleButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    isMiddleMouseScrolling = true;
                    scrollStartPoint = e.GetPosition(textEditor.TextArea);
                    scrollStartOffset = textEditor.VerticalOffset;
                    textEditor.Cursor = System.Windows.Input.Cursors.ScrollAll;
                    e.Handled = true;
                }
            };
            
            textEditor.TextArea.MouseMove += (s, e) =>
            {
                if (isMiddleMouseScrolling)
                {
                    System.Windows.Point currentPoint = e.GetPosition(textEditor.TextArea);
                    double deltaY = scrollStartPoint.Y - currentPoint.Y;
                    textEditor.ScrollToVerticalOffset(scrollStartOffset + deltaY);
                    e.Handled = true;
                }
            };
            
            textEditor.TextArea.MouseUp += (s, e) =>
            {
                if (e.MiddleButton == System.Windows.Input.MouseButtonState.Released && isMiddleMouseScrolling)
                {
                    isMiddleMouseScrolling = false;
                    textEditor.Cursor = System.Windows.Input.Cursors.IBeam;
                    e.Handled = true;
                }
            };
            
            textEditor.TextArea.MouseLeave += (s, e) =>
            {
                if (isMiddleMouseScrolling)
                {
                    isMiddleMouseScrolling = false;
                    textEditor.Cursor = System.Windows.Input.Cursors.IBeam;
                }
            };

            // Initialize TextMarkerService for conditional compilation highlighting
            textMarkerService = new TextMarkerService();
            textEditor.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
            textEditor.TextArea.TextView.LineTransformers.Add(textMarkerService);

            // Configure line number margin and hide corner control
            // Hide corner control immediately when editor is loaded (synchronous) and on layout updates
            textEditor.Loaded += (s, e) =>
            {
                // Hide corner control immediately (synchronous) to prevent white box from appearing
                HideCornerControl();
                
                // Also update scrollbar colors and hide corner again after scrollbars are styled
                if (styleManager != null)
                {
                    textEditor.Dispatcher?.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() =>
                        {
                            UpdateScrollbarColors(); // This also calls HideCornerControl internally
                        }));
                }
            };
            
            // Also hide corner control when layout updates (in case scrollbars appear/disappear)
            // Use Render priority so it happens before the frame is drawn (prevents visible white box)
            textEditor.LayoutUpdated += (s, e) =>
            {
                // Hide corner control synchronously on layout updates to prevent it from appearing
                // This ensures the corner stays hidden even during resize or scrollbar appearance
                HideCornerControl();
            };

            // Initialize code folding
            foldingStrategy = new BraceFoldingStrategy();
            foldingManager = FoldingManager.Install(textEditor.TextArea);
            textEditor.TextChanged += (s, e) => UpdateFoldings();

            // Enable brace matching (built into AvalonEdit via highlighting)
            // Brace matching is automatically enabled when syntax highlighting is loaded

            // Wire up events
            textEditor.TextChanged += (s, e) => {
                TextChanged?.Invoke(this, e);
                UpdateFoldings();
            };
            textEditor.PreviewKeyDown += TextEditor_PreviewKeyDown;
            textEditor.PreviewKeyUp += TextEditor_PreviewKeyUp;
            textEditor.MouseDown += TextEditor_MouseDown;
            textEditor.MouseDoubleClick += TextEditor_MouseDoubleClick;

            // SearchPanel disabled - using custom search dialog instead
            // searchPanel = SearchPanel.Install(textEditor);

            // Initial folding update
            UpdateFoldings();
        }

        private void UpdateFoldings()
        {
            if (foldingManager != null && foldingStrategy != null && textEditor?.Document != null)
            {
                foldingStrategy.UpdateFoldings(foldingManager, textEditor.Document);
            }
        }

        private void TextEditor_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Convert WPF KeyEventArgs to WinForms KeyEventArgs
            var key = ConvertWpfKeyToWinFormsKey(e.Key);
            var keyData = ConvertWpfModifiersToWinFormsModifiers(e.KeyboardDevice.Modifiers) | key;
            var winFormsArgs = new System.Windows.Forms.KeyEventArgs(keyData);
            KeyDown?.Invoke(this, winFormsArgs);
            if (winFormsArgs.Handled)
            {
                e.Handled = true;
            }
        }

        private void TextEditor_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Handle key press events if needed
        }

        private void TextEditor_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var point = e.GetPosition(textEditor);
            var winFormsPoint = new System.Drawing.Point((int)point.X, (int)point.Y);
            var button = ConvertWpfMouseButtonToWinFormsMouseButton(e.ChangedButton);
            var winFormsArgs = new System.Windows.Forms.MouseEventArgs(button, 1, winFormsPoint.X, winFormsPoint.Y, 0);
            MouseClick?.Invoke(this, winFormsArgs);
        }

        private void TextEditor_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var point = e.GetPosition(textEditor);
            var winFormsPoint = new System.Drawing.Point((int)point.X, (int)point.Y);
            var button = ConvertWpfMouseButtonToWinFormsMouseButton(e.ChangedButton);
            var winFormsArgs = new System.Windows.Forms.MouseEventArgs(button, 2, winFormsPoint.X, winFormsPoint.Y, 0);
            MouseDoubleClick?.Invoke(this, winFormsArgs);
        }

        // Note: Key conversion is now available in PoisonWpfHelper.ConvertWpfKey
        // Keeping this method for backward compatibility, but new code should use PoisonWpfHelper
        private Keys ConvertWpfKeyToWinFormsKey(System.Windows.Input.Key key)
        {
            // Convert common keys
            if (key >= System.Windows.Input.Key.A && key <= System.Windows.Input.Key.Z)
                return (Keys)((int)Keys.A + (key - System.Windows.Input.Key.A));
            if (key >= System.Windows.Input.Key.D0 && key <= System.Windows.Input.Key.D9)
                return (Keys)((int)Keys.D0 + (key - System.Windows.Input.Key.D0));
            if (key >= System.Windows.Input.Key.NumPad0 && key <= System.Windows.Input.Key.NumPad9)
                return (Keys)((int)Keys.NumPad0 + (key - System.Windows.Input.Key.NumPad0));

            switch (key)
            {
                case System.Windows.Input.Key.Enter: return Keys.Enter;
                case System.Windows.Input.Key.Escape: return Keys.Escape;
                case System.Windows.Input.Key.Tab: return Keys.Tab;
                case System.Windows.Input.Key.Back: return Keys.Back;
                case System.Windows.Input.Key.Delete: return Keys.Delete;
                case System.Windows.Input.Key.Insert: return Keys.Insert;
                case System.Windows.Input.Key.Home: return Keys.Home;
                case System.Windows.Input.Key.End: return Keys.End;
                case System.Windows.Input.Key.PageUp: return Keys.PageUp;
                case System.Windows.Input.Key.PageDown: return Keys.PageDown;
                case System.Windows.Input.Key.Left: return Keys.Left;
                case System.Windows.Input.Key.Right: return Keys.Right;
                case System.Windows.Input.Key.Up: return Keys.Up;
                case System.Windows.Input.Key.Down: return Keys.Down;
                case System.Windows.Input.Key.F1: return Keys.F1;
                case System.Windows.Input.Key.F2: return Keys.F2;
                case System.Windows.Input.Key.F3: return Keys.F3;
                case System.Windows.Input.Key.F4: return Keys.F4;
                case System.Windows.Input.Key.F5: return Keys.F5;
                case System.Windows.Input.Key.F6: return Keys.F6;
                case System.Windows.Input.Key.F7: return Keys.F7;
                case System.Windows.Input.Key.F8: return Keys.F8;
                case System.Windows.Input.Key.F9: return Keys.F9;
                case System.Windows.Input.Key.F10: return Keys.F10;
                case System.Windows.Input.Key.F11: return Keys.F11;
                case System.Windows.Input.Key.F12: return Keys.F12;
                default: return Keys.None;
            }
        }

        private Keys ConvertWpfModifiersToWinFormsModifiers(System.Windows.Input.ModifierKeys modifiers)
        {
            Keys result = Keys.None;
            if ((modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
                result |= Keys.Control;
            if ((modifiers & System.Windows.Input.ModifierKeys.Shift) != 0)
                result |= Keys.Shift;
            if ((modifiers & System.Windows.Input.ModifierKeys.Alt) != 0)
                result |= Keys.Alt;
            return result;
        }

        private MouseButtons ConvertWpfMouseButtonToWinFormsMouseButton(System.Windows.Input.MouseButton button)
        {
            switch (button)
            {
                case System.Windows.Input.MouseButton.Left: return MouseButtons.Left;
                case System.Windows.Input.MouseButton.Right: return MouseButtons.Right;
                case System.Windows.Input.MouseButton.Middle: return MouseButtons.Middle;
                default: return MouseButtons.None;
            }
        }

        /// <summary>
        /// Loads GSC syntax highlighting from GSC.xshd file
        /// </summary>
        public void LoadGscSyntaxHighlighting(string xshdFilePath = null)
        {
            if (textEditor == null)
                return;

            try
            {
                Stream xshdStream = null;
                
                // First, try to load from embedded resources
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = "T7CompilerGUI.Resources.GSC.xshd";
                xshdStream = assembly.GetManifestResourceStream(resourceName);
                
                // If embedded resource not found and file path provided, try file system
                if (xshdStream == null && !string.IsNullOrEmpty(xshdFilePath) && File.Exists(xshdFilePath))
                {
                    xshdStream = new FileStream(xshdFilePath, FileMode.Open, FileAccess.Read);
                }
                
                if (xshdStream == null)
                {
                    System.Diagnostics.Debug.WriteLine("GSC.xshd not found in embedded resources or file system");
                    return;
                }

                using (xshdStream)
                using (var reader = new XmlTextReader(xshdStream))
                {
                    var highlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    textEditor.SyntaxHighlighting = highlighting;
                    
                    // Verify syntax highlighting was loaded
                    if (textEditor.SyntaxHighlighting == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Warning: Syntax highlighting loaded but is null");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Syntax highlighting loaded successfully: {textEditor.SyntaxHighlighting.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load GSC.xshd: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Sets the background color and a default foreground color
        /// Syntax highlighting will override the default foreground for specific tokens
        /// This method is used as a fallback when StyleManager is not available
        /// </summary>
        public void SetColors(System.Drawing.Color backgroundColor, System.Drawing.Color foregroundColor)
        {
            if (textEditor == null)
                return;

            // Set background
            textEditor.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                backgroundColor.A, backgroundColor.R, backgroundColor.G, backgroundColor.B));
            
            // Set default foreground color - syntax highlighting will override this for specific tokens
            // This ensures text is visible even if syntax highlighting doesn't cover everything
            if (styleManager != null)
            {
                // Use theme-appropriate default text color from PoisonPaint
                System.Drawing.Color defaultTextColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                textEditor.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                    defaultTextColor.A, defaultTextColor.R, defaultTextColor.G, defaultTextColor.B));
            }
            else
            {
                // Fallback: use the provided foreground color or a neutral default
            textEditor.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                foregroundColor.A, foregroundColor.R, foregroundColor.G, foregroundColor.B));
            }
        }
        
        // Note: Visual tree helper methods are now in PoisonWpfHelper
        // Legacy methods removed - use PoisonWpfHelper.FindVisualChild<T> and PoisonWpfHelper.FindVisualChildren<T> instead
        // This reduces code duplication and provides a centralized WPF helper
        
        // Note: Corner control hiding is now handled by PoisonWpfHelper.HideScrollViewerCorner
        /// <summary>
        /// Efficiently hides the corner control using multiple approaches for maximum compatibility
        /// The corner control is the small box where horizontal and vertical scrollbars meet
        /// </summary>
        private void HideCornerControl()
        {
            if (textEditor == null) return;
            
            // Find the ScrollViewer in the visual tree
            var scrollViewer = PoisonWpfHelper.FindVisualChild<ScrollViewer>(textEditor);
            if (scrollViewer == null) return;
            
            // Ensure template is applied before trying to hide corner control
            // This prevents the white box from appearing
            try
            {
                scrollViewer.ApplyTemplate();
            }
            catch
            {
                // Template might already be applied or not available yet
            }
            
            // Use helper method which handles multiple hiding approaches
            PoisonWpfHelper.HideScrollViewerCorner(scrollViewer);
            
            // Force immediate layout update to apply hiding before next render
            // This prevents the white box from appearing in screenshots
            scrollViewer.UpdateLayout();
        }

        /// <summary>
        /// Applies Poison theme colors to the editor using WPF resource dictionaries
        /// This follows the proper WPF theming approach for AvalonEdit
        /// </summary>
        private void ApplyPoisonTheme()
        {
            if (textEditor == null || styleManager == null)
                return;

                // Get theme colors from PoisonPaint and convert to WPF
                System.Drawing.Color backgroundColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                System.Drawing.Color foregroundColor = PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                
                // Use PoisonWpfHelper for color conversion
                System.Windows.Media.Color wpfBgColor = PoisonWpfHelper.ToWpfColor(backgroundColor);
                System.Windows.Media.Color wpfFgColor = PoisonWpfHelper.ToWpfColor(foregroundColor);
                
                // Set ElementHost background to match theme (prevents white boxes)
                if (elementHost != null)
                {
                    elementHost.BackColor = backgroundColor;
                }
                
                // Apply background and foreground to editor using helper brushes
                textEditor.Background = PoisonWpfHelper.ToWpfBrush(backgroundColor);
                textEditor.Foreground = PoisonWpfHelper.ToWpfBrush(foregroundColor);
                
                // Apply WPF resources using helper
                PoisonWpfHelper.ApplyPoisonResources(textEditor, styleManager);
                
                // Create or update WPF resource dictionary for AvalonEdit components (legacy method)
                ApplyWpfResources();
            
            // Update line number margin and scrollbar colors
            // Use Background priority to avoid blocking UI updates
            if (textEditor != null && textEditor.Dispatcher != null)
            {
                textEditor.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() =>
                    {
                        UpdateLineNumberMarginColors();
                        UpdateScrollbarColors();
                        // Force refresh to ensure scrollbar is properly styled
                        RefreshScrollbar();
                    }));
            }
            else
            {
                // Fallback: try immediately
                UpdateLineNumberMarginColors();
                UpdateScrollbarColors();
                RefreshScrollbar();
            }
        }
        
        /// <summary>
        /// Applies WPF resource dictionary with Poison theme colors for AvalonEdit components
        /// This is the proper way to theme WPF controls like AvalonEdit
        /// </summary>
        private void ApplyWpfResources()
        {
            if (textEditor == null || styleManager == null)
                return;

            try
            {
                // Get theme colors
                System.Drawing.Color backgroundColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                System.Drawing.Color foregroundColor = PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                System.Drawing.Color styleColor = PoisonPaint.GetStyleColor(styleManager.Style);
                
                // Calculate line number margin background (slightly different from editor)
                int r = backgroundColor.R;
                int g = backgroundColor.G;
                int b = backgroundColor.B;
                
                if (styleManager.Theme == ReaLTaiizor.Enum.Poison.ThemeStyle.Dark)
                {
                    r = Math.Max(0, r - 10);
                    g = Math.Max(0, g - 10);
                    b = Math.Max(0, b - 10);
                }
                else
                {
                    r = Math.Min(255, r + 10);
                    g = Math.Min(255, g + 10);
                    b = Math.Min(255, b + 10);
                }
                
                System.Windows.Media.Color marginBgColor = System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b);
                System.Windows.Media.Color marginFgColor = System.Windows.Media.Color.FromRgb(foregroundColor.R, foregroundColor.G, foregroundColor.B);
                System.Windows.Media.Color wpfBgColor = System.Windows.Media.Color.FromRgb(backgroundColor.R, backgroundColor.G, backgroundColor.B);
                
                // Create resource dictionary with Poison theme colors
                var resources = new System.Windows.ResourceDictionary
                {
                    ["PoisonBackgroundBrush"] = new SolidColorBrush(wpfBgColor),
                    ["PoisonForegroundBrush"] = new SolidColorBrush(marginFgColor),
                    ["PoisonMarginBackgroundBrush"] = new SolidColorBrush(marginBgColor),
                    ["PoisonMarginForegroundBrush"] = new SolidColorBrush(marginFgColor),
                    ["PoisonStyleColorBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(styleColor.R, styleColor.G, styleColor.B))
                };
                
                // Apply resources to the text editor
                // Note: AvalonEdit doesn't directly use these, but we can reference them for custom styling
                if (textEditor.Resources == null)
                {
                    textEditor.Resources = new System.Windows.ResourceDictionary();
                }
                
                // Merge our Poison theme resources
                foreach (var key in resources.Keys)
                {
                    textEditor.Resources[key] = resources[key];
                }
                
                // Also apply to TextArea
                if (textEditor.TextArea != null && textEditor.TextArea.Resources != null)
                {
                    foreach (var key in resources.Keys)
                    {
                        textEditor.TextArea.Resources[key] = resources[key];
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying WPF resources: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Updates the line number margin colors to match Poison theme
        /// </summary>
        private void UpdateLineNumberMarginColors()
        {
            if (textEditor == null || styleManager == null)
                return;

            try
            {
                // Get theme colors
                System.Drawing.Color backgroundColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                System.Drawing.Color foregroundColor = PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                
                // Line number margin uses a slightly different background
                // Make it slightly darker/lighter than editor background
                int r = backgroundColor.R;
                int g = backgroundColor.G;
                int b = backgroundColor.B;
                
                // Adjust brightness for line number margin
                if (styleManager.Theme == ReaLTaiizor.Enum.Poison.ThemeStyle.Dark)
                {
                    // Dark theme: make margin slightly darker
                    r = Math.Max(0, r - 10);
                    g = Math.Max(0, g - 10);
                    b = Math.Max(0, b - 10);
                }
                else
                {
                    // Light theme: make margin slightly lighter
                    r = Math.Min(255, r + 10);
                    g = Math.Min(255, g + 10);
                    b = Math.Min(255, b + 10);
                }
                
                System.Windows.Media.Color marginBgColor = System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b);
                System.Windows.Media.Color marginFgColor = System.Windows.Media.Color.FromRgb(foregroundColor.R, foregroundColor.G, foregroundColor.B);
                
                // Apply colors to line number margin
                // AvalonEdit's line number margin can be styled via TextArea.LeftMargins
                // We use multiple approaches to ensure the styling works
                // First, ensure line numbers are visible
                if (textEditor != null)
                {
                    textEditor.ShowLineNumbers = true;
                }
                
                if (textEditor.TextArea?.LeftMargins != null)
                {
                    foreach (var margin in textEditor.TextArea.LeftMargins)
                    {
                        // Find LineNumberMargin and apply colors
                        string marginTypeName = margin.GetType().Name;
                        if (marginTypeName.Contains("LineNumberMargin") || marginTypeName.Contains("LineNumber"))
                        {
                            // Ensure margin is visible and apply styling
                            if (margin is System.Windows.FrameworkElement marginElement)
                            {
                                marginElement.Visibility = System.Windows.Visibility.Visible;
                                
                                // Approach 1: Set Background and Foreground properties directly via reflection
                                var bgProperty = margin.GetType().GetProperty("Background");
                                if (bgProperty != null && bgProperty.CanWrite)
                                {
                                    bgProperty.SetValue(margin, new SolidColorBrush(marginBgColor));
                                }
                                
                                var fgProperty = margin.GetType().GetProperty("Foreground");
                                if (fgProperty != null && fgProperty.CanWrite)
                                {
                                    fgProperty.SetValue(margin, new SolidColorBrush(marginFgColor));
                                }
                                
                                // Approach 2: If it's a Control (which has Background property), set properties directly
                                if (marginElement is System.Windows.Controls.Control control)
                                {
                                    control.Background = new SolidColorBrush(marginBgColor);
                                    
                                    // Also try to set via resources
                                    if (control.Resources == null)
                                    {
                                        control.Resources = new System.Windows.ResourceDictionary();
                                    }
                                    control.Resources["Background"] = new SolidColorBrush(marginBgColor);
                                    control.Resources["Foreground"] = new SolidColorBrush(marginFgColor);
                                }
                                else
                                {
                                    // Set resources for FrameworkElement (if not already a Control)
                                    if (marginElement.Resources == null)
                                    {
                                        marginElement.Resources = new System.Windows.ResourceDictionary();
                                    }
                                    marginElement.Resources["Background"] = new SolidColorBrush(marginBgColor);
                                    marginElement.Resources["Foreground"] = new SolidColorBrush(marginFgColor);
                                }
                                
                                // Approach 3: Find and style all child TextBlocks (where line numbers are actually drawn)
                                // Use PoisonWpfHelper for efficient visual tree traversal
                                foreach (var tb in PoisonWpfHelper.FindVisualChildren<System.Windows.Controls.TextBlock>(marginElement))
                                {
                                    // Apply Poison theme foreground color to line numbers
                                    tb.Foreground = new SolidColorBrush(marginFgColor);
                                    tb.Background = System.Windows.Media.Brushes.Transparent;
                                }
                                
                                // Also find and style any borders using PoisonWpfHelper
                                foreach (var border in PoisonWpfHelper.FindVisualChildren<Border>(marginElement))
                                {
                                    // Style borders to match margin background
                                    if (border.Background == null || border.Background is SolidColorBrush)
                                    {
                                        border.Background = new SolidColorBrush(marginBgColor);
                                    }
                                }
                            }
                            
                            // Approach 4: Use AvalonEdit's LineNumberMargin specific properties if available
                            try
                            {
                                // Try to access LineNumberMargin-specific styling
                                var elementTypeProperty = margin.GetType().GetProperty("ElementGenerator");
                                if (elementTypeProperty != null)
                                {
                                    // Line number margin uses an element generator, which we can't directly style
                                    // But we've already styled the visual elements above
                                }
                            }
                            catch
                            {
                                // Ignore - property may not exist
                            }
                        }
                    }
                }
                
                // Also update scrollbar colors
                UpdateScrollbarColors();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating line number margin colors: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Updates scrollbar colors to match Poison theme
        /// Uses WPF resource dictionary approach for proper theming
        /// Made internal so it can be called externally or customized
        /// </summary>
        internal void UpdateScrollbarColors()
        {
            if (textEditor == null || styleManager == null)
                return;

            try
            {
                System.Drawing.Color backgroundColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                System.Drawing.Color foregroundColor = PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                System.Drawing.Color styleColor = PoisonPaint.GetStyleColor(styleManager.Style);
                
                System.Windows.Media.Color scrollbarBgColor = System.Windows.Media.Color.FromRgb(backgroundColor.R, backgroundColor.G, backgroundColor.B);
                System.Windows.Media.Color scrollbarFgColor = System.Windows.Media.Color.FromRgb(foregroundColor.R, foregroundColor.G, foregroundColor.B);
                
                // Calculate scrollbar track color (slightly different from background)
                int r = backgroundColor.R;
                int g = backgroundColor.G;
                int b = backgroundColor.B;
                
                if (styleManager.Theme == ReaLTaiizor.Enum.Poison.ThemeStyle.Dark)
                {
                    // Dark theme: make track slightly lighter than background for visibility
                    r = Math.Min(255, r + 20);
                    g = Math.Min(255, g + 20);
                    b = Math.Min(255, b + 20);
                }
                else
                {
                    // Light theme: make track slightly darker than background for visibility
                    r = Math.Max(0, r - 20);
                    g = Math.Max(0, g - 20);
                    b = Math.Max(0, b - 20);
                }
                
                System.Windows.Media.Color trackColor = System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b);
                
                // Calculate thumb color - use Poison theme or custom color
                System.Windows.Media.Color thumbColor;
                if (UsePoisonScrollbarTheme && styleManager != null)
                {
                    // Use Poison theme accent color (Blue, Green, Purple, etc. based on Style)
                    thumbColor = System.Windows.Media.Color.FromRgb(styleColor.R, styleColor.G, styleColor.B);
                }
                else if (ScrollbarThumbColor.HasValue)
                {
                    // Use custom color if specified
                    var customColor = ScrollbarThumbColor.Value;
                    thumbColor = System.Windows.Media.Color.FromRgb(customColor.R, customColor.G, customColor.B);
                }
                else
                {
                    // Fallback: use a neutral gray
                    thumbColor = System.Windows.Media.Color.FromRgb(128, 128, 128);
                }
                
                // Find ScrollViewer in the visual tree using PoisonWpfHelper
                var scrollViewer = PoisonWpfHelper.FindVisualChild<ScrollViewer>(textEditor);
                if (scrollViewer != null)
                {
                    // Make scrollviewer background transparent - no box around scrollbar
                    scrollViewer.Background = System.Windows.Media.Brushes.Transparent;
                    
                    // Try to find and style scrollbar elements using PoisonWpfHelper
                    var horizontalScrollBar = PoisonWpfHelper.FindVisualChild<ScrollBar>(
                        scrollViewer, 
                        sb => sb.Orientation == System.Windows.Controls.Orientation.Horizontal);
                    var verticalScrollBar = PoisonWpfHelper.FindVisualChild<ScrollBar>(
                        scrollViewer, 
                        sb => sb.Orientation == System.Windows.Controls.Orientation.Vertical);
                    
                    // Configure horizontal scrollbar - hide it completely for code editors
                    if (horizontalScrollBar != null)
                    {
                        // Hide horizontal scrollbar entirely for code editors
                        horizontalScrollBar.Visibility = System.Windows.Visibility.Collapsed;
                        horizontalScrollBar.IsEnabled = false;
                        horizontalScrollBar.Opacity = 0;
                        horizontalScrollBar.IsHitTestVisible = false;
                        horizontalScrollBar.Background = System.Windows.Media.Brushes.Transparent;
                    }
                    
                    // Style vertical scrollbar - make it invisible except for thumb (thumb-only style)
                    if (verticalScrollBar != null)
                    {
                        // Keep scrollbar enabled and visible so it can be used, but style to show only thumb
                        verticalScrollBar.Visibility = System.Windows.Visibility.Visible;
                        verticalScrollBar.IsEnabled = true;
                        verticalScrollBar.Opacity = 1.0;
                        verticalScrollBar.IsHitTestVisible = true;
                        // Make scrollbar background transparent - no white box around it
                        verticalScrollBar.Background = System.Windows.Media.Brushes.Transparent;
                        // Style the scrollbar to show only thumb (hide track and arrows)
                        StyleScrollBar(verticalScrollBar, scrollbarBgColor, trackColor, scrollbarFgColor, thumbColor);
                    }
                    
                    // Hide the corner control (the box where scrollbars meet) - do this after styling
                    // This must be done even if horizontal scrollbar is hidden, as the corner may still appear
                    // Hide it immediately (synchronous) to prevent white box from appearing
                    HideCornerControl();
                    
                    // Force layout update to ensure corner control hiding is applied before next render
                    scrollViewer.UpdateLayout();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating scrollbar colors: {ex.Message}");
            }
        }

        /// <summary>
        /// Styles a WPF ScrollBar control with Poison theme colors
        /// Made internal so it can be accessed/customized if needed
        /// Hides track and arrows completely, only shows the draggable thumb
        /// </summary>
        internal void StyleScrollBar(ScrollBar scrollBar, System.Windows.Media.Color bgColor, System.Windows.Media.Color trackColor, System.Windows.Media.Color fgColor, System.Windows.Media.Color thumbColor)
        {
            if (scrollBar == null) return;
            
            try
            {
                // Make scrollbar background transparent - no box around it
                scrollBar.Background = System.Windows.Media.Brushes.Transparent;
                
                // Make the entire scrollbar track background transparent
                // This ensures no track background is visible, only the thumb
                scrollBar.Opacity = 1.0; // Keep scrollbar visible for thumb
                
                // Style the scrollbar track
                // Track is a FrameworkElement (not Control), so we style its child elements instead
                var track = PoisonWpfHelper.FindVisualChild<Track>(scrollBar);
                if (track != null)
                {
                    // Make track background completely transparent - no visible track
                    // We only want the thumb visible, not the track background
                    // Note: Track is a FrameworkElement, not a Control, so it doesn't have a Background property
                    // We'll make it transparent by hiding its child elements (rectangles, borders) below
                    // First, style the thumb (the draggable part) - this is what we want visible
                    if (track.Thumb != null)
                    {
                        // Ensure thumb is visible first
                        track.Thumb.Visibility = System.Windows.Visibility.Visible;
                        track.Thumb.IsEnabled = true;
                        track.Thumb.Opacity = ScrollbarThumbOpacity; // Use configurable opacity
                        
                        // Style the thumb with Poison theme or custom color
                        var thumbBrush = new SolidColorBrush(thumbColor);
                        thumbBrush.Opacity = ScrollbarThumbOpacity; // Use configurable opacity
                        track.Thumb.Background = thumbBrush;
                        track.Thumb.BorderBrush = thumbBrush;
                        track.Thumb.BorderThickness = new System.Windows.Thickness(0); // No border
                        
                        // Ensure thumb has a minimum size so it's always visible
                        if (track.Thumb.MinHeight == 0)
                            track.Thumb.MinHeight = 10;
                        if (track.Thumb.MinWidth == 0)
                            track.Thumb.MinWidth = 10;
                    }
                    
                    // Completely hide the decrease and increase buttons (arrows) - remove them completely
                    if (track.DecreaseRepeatButton != null)
                    {
                        track.DecreaseRepeatButton.Visibility = System.Windows.Visibility.Collapsed;
                        track.DecreaseRepeatButton.IsEnabled = false;
                        track.DecreaseRepeatButton.Opacity = 0;
                        track.DecreaseRepeatButton.Height = 0;
                        track.DecreaseRepeatButton.Width = 0;
                        track.DecreaseRepeatButton.IsHitTestVisible = false;
                        // Also try to remove from visual tree if possible
                        if (track.DecreaseRepeatButton.Parent != null)
                        {
                            try
                            {
                                var parent = track.DecreaseRepeatButton.Parent as System.Windows.Controls.Panel;
                                parent?.Children.Remove(track.DecreaseRepeatButton);
                            }
                            catch { }
                        }
                    }
                    if (track.IncreaseRepeatButton != null)
                    {
                        track.IncreaseRepeatButton.Visibility = System.Windows.Visibility.Collapsed;
                        track.IncreaseRepeatButton.IsEnabled = false;
                        track.IncreaseRepeatButton.Opacity = 0;
                        track.IncreaseRepeatButton.Height = 0;
                        track.IncreaseRepeatButton.Width = 0;
                        track.IncreaseRepeatButton.IsHitTestVisible = false;
                        // Also try to remove from visual tree if possible
                        if (track.IncreaseRepeatButton.Parent != null)
                        {
                            try
                            {
                                var parent = track.IncreaseRepeatButton.Parent as System.Windows.Controls.Panel;
                                parent?.Children.Remove(track.IncreaseRepeatButton);
                            }
                            catch { }
                        }
                    }
                    
                    // Hide all Rectangle elements in the track (the white box/track background)
                    // Make them transparent so only the thumb is visible
                    if (track is System.Windows.FrameworkElement trackElement2)
                    {
                        foreach (var rect in PoisonWpfHelper.FindVisualChildren<Rectangle>(trackElement2))
                        {
                            // Check if this rectangle is part of the thumb - if so, don't hide it
                            // The thumb itself might contain rectangles, so we need to be careful
                            var parent = System.Windows.Media.VisualTreeHelper.GetParent(rect);
                            bool isPartOfThumb = false;
                            
                            // Check if rectangle is a child of the thumb
                            while (parent != null)
                            {
                                if (parent == track.Thumb)
                                {
                                    isPartOfThumb = true;
                                    break;
                                }
                                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
                            }
                            
                            // Only hide rectangles that are NOT part of the thumb (track background)
                            if (!isPartOfThumb)
                            {
                                rect.Fill = System.Windows.Media.Brushes.Transparent;
                                rect.Stroke = System.Windows.Media.Brushes.Transparent;
                                rect.Opacity = 0;
                                rect.Visibility = System.Windows.Visibility.Collapsed;
                                rect.IsHitTestVisible = false; // Make sure it's not interactive
                            }
                        }
                        
                        // Also hide any Border elements that might create the box (but not thumb borders)
                        foreach (var border in PoisonWpfHelper.FindVisualChildren<Border>(trackElement2))
                        {
                            // Check if border is part of thumb
                            var parent = System.Windows.Media.VisualTreeHelper.GetParent(border);
                            bool isPartOfThumb = false;
                            
                            while (parent != null)
                            {
                                if (parent == track.Thumb)
                                {
                                    isPartOfThumb = true;
                                    break;
                                }
                                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
                            }
                            
                            // Only hide borders that are NOT part of the thumb (track background)
                            if (!isPartOfThumb)
                            {
                                border.Background = System.Windows.Media.Brushes.Transparent;
                                border.BorderBrush = System.Windows.Media.Brushes.Transparent;
                                border.BorderThickness = new System.Windows.Thickness(0);
                                border.Opacity = 0;
                                border.Visibility = System.Windows.Visibility.Collapsed;
                                border.IsHitTestVisible = false; // Make sure it's not interactive
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error styling scrollbar: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Forces a refresh of the scrollbar to ensure Poison theme colors are applied
        /// </summary>
        private void RefreshScrollbar()
        {
            if (textEditor == null || styleManager == null)
                return;

            try
            {
                // Re-apply scrollbar colors to ensure they're updated
                UpdateScrollbarColors();
                
                // Also force a visual update using PoisonWpfHelper
                var scrollViewer = PoisonWpfHelper.FindVisualChild<ScrollViewer>(textEditor);
                if (scrollViewer != null)
                {
                    var verticalScrollBar = PoisonWpfHelper.FindVisualChild<ScrollBar>(
                        scrollViewer, 
                        sb => sb.Orientation == System.Windows.Controls.Orientation.Vertical);
                    
                    if (verticalScrollBar != null)
                    {
                        // Force scrollbar to refresh by invalidating and updating
                        verticalScrollBar.InvalidateVisual();
                        verticalScrollBar.UpdateLayout();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing scrollbar: {ex.Message}");
            }
        }

        /// <summary>
        /// Scrolls to a specific line
        /// </summary>
        public void ScrollToLine(int lineNumber)
        {
            if (textEditor != null && lineNumber > 0 && lineNumber <= textEditor.Document.LineCount)
            {
                textEditor.ScrollToLine(lineNumber);
            }
        }

        /// <summary>
        /// Gets the text of a specific line
        /// </summary>
        public string GetLineText(int lineNumber)
        {
            if (textEditor?.Document == null || lineNumber < 1 || lineNumber > textEditor.Document.LineCount)
                return "";

            var line = textEditor.Document.GetLineByNumber(lineNumber);
            return textEditor.Document.GetText(line.Offset, line.Length);
        }

        /// <summary>
        /// Gets the total number of lines
        /// </summary>
        public int LineCount => textEditor?.Document?.LineCount ?? 0;

        /// <summary>
        /// Undo the last action
        /// </summary>
        public void Undo()
        {
            if (textEditor?.Document?.UndoStack != null)
            {
                textEditor.Document.UndoStack.Undo();
            }
        }

        /// <summary>
        /// Redo the last undone action
        /// </summary>
        public void Redo()
        {
            if (textEditor?.Document?.UndoStack != null)
            {
                textEditor.Document.UndoStack.Redo();
            }
        }

        /// <summary>
        /// Check if undo is available
        /// </summary>
        public bool CanUndo => textEditor?.Document?.UndoStack?.CanUndo ?? false;

        /// <summary>
        /// Check if redo is available
        /// </summary>
        public bool CanRedo => textEditor?.Document?.UndoStack?.CanRedo ?? false;

        // ScintillaNET compatibility properties and methods
        public int CurrentPosition
        {
            get
            {
                if (textEditor?.TextArea?.Caret == null)
                    return 0;
                return textEditor.Document.GetOffset(textEditor.TextArea.Caret.Line, textEditor.TextArea.Caret.Column);
            }
            set
            {
                if (textEditor?.Document != null && value >= 0 && value < textEditor.Document.TextLength)
                {
                    var location = textEditor.Document.GetLocation(value);
                    textEditor.TextArea.Caret.Line = location.Line;
                    textEditor.TextArea.Caret.Column = location.Column;
                }
            }
        }

        // Search compatibility (using SearchPanel internally)
        private int targetStart = 0;
        private int targetEnd = 0;
        private string lastSearchText = "";
        private bool lastMatchCase = false;
        private bool lastWholeWord = false;

        public int TargetStart
        {
            get => targetStart;
            set => targetStart = value;
        }

        public int TargetEnd
        {
            get => targetEnd;
            set => targetEnd = value;
        }

        // SearchFlags enum compatibility
        [Flags]
        public enum SearchFlagsEnum
        {
            None = 0,
            MatchCase = 1,
            WholeWord = 2
        }

        private SearchFlagsEnum currentSearchFlags = SearchFlagsEnum.None;
        public SearchFlagsEnum SearchFlags
        {
            get => currentSearchFlags;
            set
            {
                currentSearchFlags = value;
                lastMatchCase = (value & SearchFlagsEnum.MatchCase) != 0;
                lastWholeWord = (value & SearchFlagsEnum.WholeWord) != 0;
            }
        }

        public int SearchInTarget(string searchText)
        {
            if (textEditor?.Document == null || string.IsNullOrEmpty(searchText))
                return -1;

            lastSearchText = searchText;
            string text = textEditor.Document.Text;
            
            // Ensure target range is valid
            if (targetStart < 0) targetStart = 0;
            if (targetEnd > text.Length) targetEnd = text.Length;
            if (targetStart >= targetEnd) return -1;

            string searchIn = text.Substring(targetStart, targetEnd - targetStart);
            StringComparison comparison = lastMatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            
            int foundPos = -1;
            if (lastWholeWord)
            {
                // Whole word search
                string pattern = @"\b" + Regex.Escape(searchText) + @"\b";
                var match = System.Text.RegularExpressions.Regex.Match(searchIn, pattern, 
                    lastMatchCase ? System.Text.RegularExpressions.RegexOptions.None : System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    foundPos = targetStart + match.Index;
                }
            }
            else
            {
                // Simple search
                int index = searchIn.IndexOf(searchText, comparison);
                if (index >= 0)
                {
                    foundPos = targetStart + index;
                }
            }

            if (foundPos >= 0)
            {
                // Update target to the found position
                TargetStart = foundPos;
                TargetEnd = foundPos + searchText.Length;
            }

            return foundPos;
        }

        public void SetSelection(int start, int end)
        {
            if (textEditor?.Document == null)
                return;

            if (start < 0) start = 0;
            if (end > textEditor.Document.TextLength) end = textEditor.Document.TextLength;
            if (start > end) { (start, end) = (end, start); }

            var startLoc = textEditor.Document.GetLocation(start);
            var endLoc = textEditor.Document.GetLocation(end);
            
            // Set selection using TextEditor's SelectionStart and SelectionLength
            textEditor.SelectionStart = start;
            textEditor.SelectionLength = end - start;
            textEditor.TextArea.Caret.Line = endLoc.Line;
            textEditor.TextArea.Caret.Column = endLoc.Column;
        }

        public void ScrollCaret()
        {
            textEditor?.ScrollToLine(textEditor.TextArea.Caret.Line);
        }

        public void EmptyUndoBuffer()
        {
            if (textEditor?.Document?.UndoStack != null)
            {
                textEditor.Document.UndoStack.ClearAll();
            }
        }

        public void SetSelectionBackColor(bool _useSelection, System.Drawing.Color _color)
            {
                // AvalonEdit uses a different selection highlighting system
                // We can set the selection background via the highlighting system
                // For now, this is handled by the default selection highlighting
        }

        public void GotoPosition(int position)
        {
            if (textEditor?.Document != null && position >= 0 && position < textEditor.Document.TextLength)
            {
                var location = textEditor.Document.GetLocation(position);
                textEditor.TextArea.Caret.Line = location.Line;
                textEditor.TextArea.Caret.Column = location.Column;
                textEditor.ScrollToLine(location.Line);
            }
        }

        public void ReplaceTarget(string replacement)
        {
            if (textEditor?.Document != null && !string.IsNullOrEmpty(replacement))
            {
                int start = TargetStart;
                int end = TargetEnd;
                if (start >= 0 && end > start && end <= textEditor.Document.TextLength)
                {
                    textEditor.Document.Replace(start, end - start, replacement);
                }
            }
        }

        public void ReplaceSelection(string replacement)
        {
            if (textEditor?.TextArea?.Selection != null && !textEditor.TextArea.Selection.IsEmpty)
            {
                SelectedText = replacement ?? "";
            }
        }

        public new void Invalidate()
        {
            elementHost?.Invalidate();
        }

        public new void Update()
        {
            elementHost?.Update();
        }

        public new void Refresh()
        {
            elementHost?.Refresh();
        }

        // Compatibility methods for conditional compilation indicators
        // Note: AvalonEdit uses a different highlighting system, so these are simplified
        private int currentIndicator = 0;
        public int IndicatorCurrent
        {
            get => currentIndicator;
            set => currentIndicator = value;
        }

        public void IndicatorClearRange(int start, int length)
        {
            if (textMarkerService != null && textEditor?.Document != null)
            {
                // Clear all markers in the specified range
                var markersToRemove = new List<TextMarker>();
                foreach (var marker in textMarkerService.GetMarkersAtOffset(start))
                {
                    if (marker.StartOffset < start + length && marker.EndOffset > start)
                    {
                        markersToRemove.Add(marker);
                    }
                }
                foreach (var marker in markersToRemove)
                {
                    textMarkerService.Remove(marker);
                }
            }
        }

        public void IndicatorFillRange(int start, int length)
        {
            if (textMarkerService != null && textEditor?.Document != null && length > 0)
            {
                int end = Math.Min(start + length, textEditor.Document.TextLength);
                if (start >= 0 && end > start)
                {
                    // Create a marker for inactive code (grayed out)
                    var marker = new TextMarker(start, end - start)
                    {
                        BackgroundColor = System.Windows.Media.Color.FromArgb(150, 128, 128, 128), // Semi-transparent gray
                        ForegroundColor = System.Windows.Media.Color.FromArgb(200, 128, 128, 128) // Grayed out text
                    };
                    textMarkerService.Add(marker);
                }
            }
        }

        /// <summary>
        /// Clears all text markers (for conditional compilation highlighting)
        /// </summary>
        public void ClearAllMarkers()
        {
            if (textMarkerService != null && textEditor?.Document != null)
            {
                textMarkerService.RemoveAll(m => true);
                // Force redraw of entire document
                if (textEditor.TextArea?.TextView != null)
                {
                    textEditor.TextArea.TextView.Redraw();
                }
            }
        }

        // Compatibility property for Lines collection
        public EditorLinesCollection Lines
        {
            get
            {
                if (textEditor?.Document == null)
                    return new EditorLinesCollection(null);
                return new EditorLinesCollection(textEditor.Document);
            }
        }

        // Helper class to provide Lines collection compatibility
        public class EditorLinesCollection
        {
            private readonly ICSharpCode.AvalonEdit.Document.TextDocument document;

            public EditorLinesCollection(ICSharpCode.AvalonEdit.Document.TextDocument doc)
            {
                document = doc;
            }

            public int Count => document?.LineCount ?? 0;

            public EditorLine this[int index]
            {
                get
                {
                    if (document == null || index < 0 || index >= document.LineCount)
                        return null;
                    var line = document.GetLineByNumber(index + 1); // AvalonEdit is 1-based
                    return new EditorLine(line, document);
                }
            }
        }

        public class EditorLine
        {
            private readonly ICSharpCode.AvalonEdit.Document.DocumentLine line;
            private readonly ICSharpCode.AvalonEdit.Document.TextDocument document;

            public EditorLine(ICSharpCode.AvalonEdit.Document.DocumentLine docLine, ICSharpCode.AvalonEdit.Document.TextDocument doc)
            {
                line = docLine;
                document = doc;
            }

            public string Text
            {
                get
                {
                    if (line == null || document == null)
                        return "";
                    return document.GetText(line.Offset, line.Length);
                }
            }

            public int Position => line?.Offset ?? 0;
            public int EndPosition => line?.EndOffset ?? 0;
        }

        /// <summary>
        /// Gets or sets the font size (zoom level)
        /// </summary>
        public double FontSize
        {
            get => textEditor?.FontSize ?? 12;
            set
            {
                if (textEditor != null)
                {
                    textEditor.FontSize = value;
                }
            }
        }

        /// <summary>
        /// Zoom in (increase font size)
        /// </summary>
        public void ZoomIn()
        {
            if (textEditor != null)
            {
                textEditor.FontSize = Math.Min(textEditor.FontSize + 1, 72);
            }
        }

        /// <summary>
        /// Zoom out (decrease font size)
        /// </summary>
        public void ZoomOut()
        {
            if (textEditor != null)
            {
                textEditor.FontSize = Math.Max(textEditor.FontSize - 1, 6);
            }
        }

        /// <summary>
        /// Reset zoom to default (12pt)
        /// </summary>
        public void ZoomReset()
        {
            if (textEditor != null)
            {
                textEditor.FontSize = 12;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clear text marker service (clears all markers to free memory)
                if (textMarkerService != null)
                {
                    try
                    {
                        textMarkerService.Clear();
                        // Remove from text area services if still attached
                        if (textEditor?.TextArea != null && textEditor.TextArea.TextView != null)
                        {
                            var services = textEditor.TextArea.TextView.Services;
                            if (services != null)
                            {
                                services.RemoveService(typeof(TextMarkerService));
                            }
                        }
                    }
                    catch { }
                    textMarkerService = null;
                }
                
                // Clear folding manager reference (doesn't implement IDisposable)
                // The FoldingManager will be cleaned up when the document is disposed
                foldingManager = null;
                foldingStrategy = null;
                
                // Clear style manager reference
                styleManager = null;
                
                // Clear text editor reference (WPF object, will be cleaned up when ElementHost disposes)
                // Event handlers attached to textEditor are lambdas that will be cleaned up automatically
                // when ElementHost disposes and the WPF visual tree is torn down
                textEditor = null;
                
                // Dispose ElementHost - this will dispose all WPF content including TextEditor
                // This is critical for memory cleanup as WPF objects can hold references
                // ElementHost.Dispose() will properly clean up the entire WPF visual tree
                if (elementHost != null)
                {
                    try
                    {
                        elementHost.Dispose();
                    }
                    catch { }
                    elementHost = null;
                }
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Improved brace folding strategy for GSC code (supports { }, [ ], ( ))
    /// Handles nested braces, ignores strings and comments
    /// </summary>
    public class BraceFoldingStrategy
    {
        private struct BraceInfo
        {
            public int Offset;
            public char OpeningBrace;
            public int StartLine;
        }

        public void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            if (manager == null || document == null)
                return;

            var newFoldings = CreateNewFoldings(document, out int firstErrorOffset);
            manager.UpdateFoldings(newFoldings, firstErrorOffset);
        }

        private IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
        {
            firstErrorOffset = -1;
            var newFoldings = new List<NewFolding>();

            var braceStack = new Stack<BraceInfo>();
            bool inSingleLineComment = false;
            bool inMultiLineComment = false;
            bool inSingleQuoteString = false;
            bool inDoubleQuoteString = false;
            char prevChar = '\0';

            for (int i = 0; i < document.TextLength; i++)
            {
                char c = document.GetCharAt(i);
                
                // Handle single-line comments (//)
                if (!inMultiLineComment && !inSingleQuoteString && !inDoubleQuoteString && 
                    prevChar == '/' && c == '/')
                {
                    inSingleLineComment = true;
                }
                if (inSingleLineComment && (c == '\n' || c == '\r'))
                {
                    inSingleLineComment = false;
                }

                // Handle multi-line comments (/* */)
                if (!inSingleLineComment && !inSingleQuoteString && !inDoubleQuoteString)
                {
                    if (prevChar == '/' && c == '*')
                    {
                        inMultiLineComment = true;
                    }
                    if (inMultiLineComment && prevChar == '*' && c == '/')
                    {
                        inMultiLineComment = false;
                        // Skip the '/' character so it's not processed as a brace
                        prevChar = c;
                        continue;
                    }
                }

                // Handle strings - ignore braces inside strings
                if (!inSingleLineComment && !inMultiLineComment)
                {
                    // Single quote strings (check for escaped quotes)
                    if (c == '\'' && prevChar != '\\')
                    {
                        inSingleQuoteString = !inSingleQuoteString;
                    }
                    // Double quote strings (check for escaped quotes)
                    if (c == '"' && prevChar != '\\')
                    {
                        inDoubleQuoteString = !inDoubleQuoteString;
                    }
                }

                // Track previous character for next iteration
                prevChar = c;

                // Skip processing if inside comment or string
                if (inSingleLineComment || inMultiLineComment || inSingleQuoteString || inDoubleQuoteString)
                    continue;

                // Get current line number for the character
                int currentLine = document.GetLineByOffset(i).LineNumber;
                int startLine = -1;

                // Check for opening braces
                if (c == '{' || c == '[' || c == '(')
                {
                    braceStack.Push(new BraceInfo
                    {
                        Offset = i,
                        OpeningBrace = c,
                        StartLine = currentLine
                    });
                }
                // Check for closing braces
                else if (c == '}' || c == ']' || c == ')')
                {
                    if (braceStack.Count > 0)
                    {
                        var braceInfo = braceStack.Pop();
                        char expectedClosing = GetClosingBrace(braceInfo.OpeningBrace);
                        
                        if (c == expectedClosing)
                        {
                            startLine = braceInfo.StartLine;
                            int endLine = currentLine;
                            
                            // Only create folding if opening and closing are on different lines
                            // or if there's content between them (more than just whitespace)
                            if (startLine < endLine || HasNonWhitespaceContent(document, braceInfo.Offset + 1, i))
                            {
                                // Create folding from after the opening brace to before the closing brace
                                // This will fold the content between the braces
                                int foldingStart = braceInfo.Offset + 1;
                                int foldingEnd = i;
                                
                                // Only create folding if there's actual content to fold
                                if (foldingEnd > foldingStart)
                                {
                                    newFoldings.Add(new NewFolding(foldingStart, foldingEnd));
                                }
                            }
                        }
                        else
                        {
                            // Mismatched brace - push it back and mark error
                            braceStack.Push(braceInfo);
                            if (firstErrorOffset < 0)
                            {
                                firstErrorOffset = i;
                            }
                        }
                    }
                    else if (firstErrorOffset < 0)
                    {
                        firstErrorOffset = i;
                    }
                }
            }

            newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            return newFoldings;
        }

        private bool HasNonWhitespaceContent(TextDocument document, int startOffset, int endOffset)
        {
            if (startOffset >= endOffset)
                return false;

            for (int i = startOffset; i < endOffset; i++)
            {
                char c = document.GetCharAt(i);
                if (!char.IsWhiteSpace(c) && c != '\n' && c != '\r')
                    return true;
            }
            return false;
        }

        private char GetClosingBrace(char openingBrace)
        {
            switch (openingBrace)
            {
                case '{': return '}';
                case '[': return ']';
                case '(': return ')';
                default: return '\0';
            }
        }
    }
}

