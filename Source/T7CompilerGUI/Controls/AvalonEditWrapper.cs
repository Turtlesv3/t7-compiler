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
using ScrollViewer = System.Windows.Controls.ScrollViewer;
using Border = System.Windows.Controls.Border;
using Grid = System.Windows.Controls.Grid;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace T7CompilerGUI.Controls
{
    /// <summary>
    /// Wraps AvalonEdit TextEditor in an ElementHost for use in WinForms
    /// Provides a similar interface to ScintillaNET for easier migration
    /// </summary>
    public class AvalonEditWrapper : System.Windows.Forms.UserControl
    {
        private System.Windows.Forms.Integration.ElementHost elementHost;
        private TextEditor textEditor;
        // SearchPanel disabled - using custom search dialog instead
        // private SearchPanel searchPanel;
        private TextMarkerService textMarkerService;
        private FoldingManager foldingManager;
        private BraceFoldingStrategy foldingStrategy;
        private ReaLTaiizor.Manager.PoisonStyleManager styleManager;

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
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Disable AutoScroll to prevent unwanted scrollbars
            this.AutoScroll = false;

            // Create ElementHost
            elementHost = new System.Windows.Forms.Integration.ElementHost
            {
                Dock = DockStyle.Fill,
                Child = null,
                BackColor = System.Drawing.Color.Transparent // Make transparent so Poison background shows through
            };

            // Create AvalonEdit TextEditor
            textEditor = new TextEditor
            {
                ShowLineNumbers = true,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                WordWrap = false,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Hidden,
                Options = {
                    EnableEmailHyperlinks = false,
                    EnableHyperlinks = false,
                    EnableTextDragDrop = true,
                    AllowScrollBelowDocument = false,
                    CutCopyWholeLine = true,
                    IndentationSize = 4,
                    ConvertTabsToSpaces = false,
                    EnableRectangularSelection = true,
                    EnableVirtualSpace = false
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
            // The corner control appears at the intersection of line number margin and scrollbar area
            // Hide it using multiple approaches to ensure it's caught
            
            // Approach 1: Hide when TextArea is loaded
            textEditor.TextArea.Loaded += (s, e) =>
            {
                HideCornerControl();
            };
            
            // Approach 2: Also hide after layout is updated (in case it appears later)
            textEditor.TextArea.LayoutUpdated += (s, e) =>
            {
                HideCornerControl();
            };
            
            // Approach 3: Hide when the editor is fully rendered
            textEditor.Loaded += (s, e) =>
            {
                HideCornerControl();
            };
            
            // Approach 4: Hide using Dispatcher to ensure visual tree is fully built
            textEditor.Loaded += (s, e) =>
            {
                // Use Dispatcher to ensure visual tree is fully built
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Loaded,
                    new Action(() => {
                        HideCornerControl();
                        // Also try again after a short delay to catch late-rendered elements
                        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() => {
                                HideCornerControl();
                            }));
                    }));
            };
            
            // Approach 5: Hide on every layout update to catch it if it reappears
            textEditor.LayoutUpdated += (s, e) =>
            {
                HideCornerControl();
            };

            // Initialize code folding
            foldingStrategy = new BraceFoldingStrategy();
            foldingManager = FoldingManager.Install(textEditor.TextArea);
            textEditor.TextChanged += (s, e) => UpdateFoldings();

            // Enable brace matching (built into AvalonEdit via highlighting)
            // Brace matching is automatically enabled when syntax highlighting is loaded

            // Set ElementHost child
            elementHost.Child = textEditor;

            // Add ElementHost to this control
            this.Controls.Add(elementHost);

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

            this.ResumeLayout(false);
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
                // Use theme-appropriate default text color
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
        
        // Helper methods to find visual children in WPF
        private static T FindVisualChild<T>(System.Windows.DependencyObject parent, Func<T, bool> predicate = null) where T : System.Windows.DependencyObject
        {
            if (parent == null) return null;
            
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T t && (predicate == null || predicate(t)))
                    return t;
                
                var childOfChild = FindVisualChild<T>(child, predicate);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
        
        private static IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            if (parent == null) yield break;
            
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    yield return t;
                
                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
        
        // Helper method to hide the corner control (white L bracket)
        private void HideCornerControl()
        {
            if (textEditor == null) return;
            
            try
            {
                // Search the entire visual tree starting from textEditor (not just TextArea)
                // The corner control is in the ScrollViewer, which is a parent of TextArea
                var allBorders = FindVisualChildren<System.Windows.Controls.Border>(textEditor).ToList();
                
                // Also search in TextArea specifically
                if (textEditor.TextArea != null)
                {
                    allBorders.AddRange(FindVisualChildren<Border>(textEditor.TextArea).ToList());
                }
                
                foreach (var border in allBorders)
                {
                    // Hide any border that could be the corner control
                    // Check by size (small borders at corners), name, or position
                    bool isCornerControl = false;
                    
                    // Check by name first (most reliable)
                    if (border.Name != null)
                    {
                        string name = border.Name.ToLowerInvariant();
                        if (name.Contains("corner") || name == "part_cornercontrol" || name.Contains("part_"))
                        {
                            isCornerControl = true;
                        }
                    }
                    
                    // Check by size (corner controls are typically small)
                    if (!isCornerControl && (border.Width < 30 && border.Height < 30))
                    {
                        // Check if it's positioned at a corner (top-right area where line numbers end)
                        try
                        {
                            if (textEditor.TextArea != null)
                            {
                                var position = border.TransformToAncestor(textEditor.TextArea).Transform(new System.Windows.Point(0, 0));
                                
                                // Line number margin is typically 40-60 pixels wide
                                // Corner control appears at top-right of line number area (around X=40-70, Y=0-30)
                                if (position.X >= 30 && position.X <= 80 && position.Y >= 0 && position.Y <= 30)
                                {
                                    isCornerControl = true;
                                }
                            }
                        }
                        catch
                        {
                            // If we can't determine position, check if it's a small border with no name
                            // (corner controls often have no explicit name)
                            if (string.IsNullOrEmpty(border.Name) && border.Width < 20 && border.Height < 20)
                            {
                                isCornerControl = true;
                            }
                        }
                    }
                    
                    if (isCornerControl)
                    {
                        border.Visibility = System.Windows.Visibility.Collapsed;
                        border.IsHitTestVisible = false;
                        border.Opacity = 0;
                        border.Width = 0;
                        border.Height = 0;
                        border.Margin = new System.Windows.Thickness(0);
                    }
                }
                
                // Also search for ScrollViewer and hide its corner control directly
                var scrollViewer = FindVisualChild<ScrollViewer>(textEditor);
                if (scrollViewer != null)
                {
                    // Search for any small controls in the ScrollViewer (corner control could be various types)
                    var scrollViewerChildren = FindVisualChildren<System.Windows.FrameworkElement>(scrollViewer).ToList();
                    foreach (var child in scrollViewerChildren)
                    {
                        // Hide small elements that could be the corner control
                        if (child.Width < 30 && child.Height < 30 && child.Width > 0 && child.Height > 0)
                        {
                            // Check if it's positioned at the corner
                            try
                            {
                                var position = child.TransformToAncestor(scrollViewer).Transform(new System.Windows.Point(0, 0));
                                // Corner is typically at top-right
                                if (position.X > scrollViewer.ActualWidth - 50 && position.Y < 50)
                                {
                                    child.Visibility = System.Windows.Visibility.Collapsed;
                                    child.IsHitTestVisible = false;
                                    child.Opacity = 0;
                                }
                            }
                            catch
                            {
                                // If we can't determine position, hide small unnamed elements
                                if (string.IsNullOrEmpty(child.Name) || child.Name.Contains("Corner") || child.Name.Contains("PART_"))
                                {
                                    child.Visibility = System.Windows.Visibility.Collapsed;
                                    child.IsHitTestVisible = false;
                                    child.Opacity = 0;
                                }
                            }
                        }
                    }
                }
                
                // Also search for Rectangle shapes (corner control might be a Rectangle)
                var allRectangles = FindVisualChildren<Rectangle>(textEditor).ToList();
                foreach (var rect in allRectangles)
                {
                    if (rect.Width < 30 && rect.Height < 30 && rect.Width > 0 && rect.Height > 0)
                    {
                        rect.Visibility = System.Windows.Visibility.Collapsed;
                        rect.IsHitTestVisible = false;
                        rect.Opacity = 0;
                    }
                }
                
                // Search for Grid controls that might contain the corner (corner is often in a Grid)
                var allGrids = FindVisualChildren<System.Windows.Controls.Grid>(textEditor).ToList();
                foreach (var grid in allGrids)
                {
                    // Look for small grids that might be the corner container
                    if (grid.Width < 30 && grid.Height < 30 && grid.Width > 0 && grid.Height > 0)
                    {
                        // Hide the entire grid if it's at the corner position
                        try
                        {
                            if (textEditor.TextArea != null)
                            {
                                var position = grid.TransformToAncestor(textEditor.TextArea).Transform(new System.Windows.Point(0, 0));
                                if (position.X >= 30 && position.X <= 80 && position.Y >= 0 && position.Y <= 30)
                                {
                                    grid.Visibility = System.Windows.Visibility.Collapsed;
                                    grid.IsHitTestVisible = false;
                                    grid.Opacity = 0;
                                }
                            }
                        }
                        catch
                        {
                            // If we can't determine position, hide small unnamed grids
                            if (string.IsNullOrEmpty(grid.Name))
                            {
                                grid.Visibility = System.Windows.Visibility.Collapsed;
                                grid.IsHitTestVisible = false;
                                grid.Opacity = 0;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        /// <summary>
        /// Applies Poison theme colors to the editor and SearchPanel
        /// Only sets background - syntax highlighting controls text colors
        /// </summary>
        private void ApplyPoisonTheme()
        {
            if (textEditor == null || styleManager == null)
                return;

            // Get theme background color from PoisonPaint
            System.Drawing.Color backgroundColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
            
            // Set ElementHost background to match theme (prevents white boxes)
            if (elementHost != null)
            {
                elementHost.BackColor = backgroundColor;
            }
            
            // Apply only background to editor - syntax highlighting handles text colors
            SetColors(backgroundColor, System.Drawing.Color.Transparent);
            
            // SearchPanel disabled - using custom search dialog instead
            // SearchPanel styling code removed
            
            // Update line number margin colors
            UpdateLineNumberMarginColors();
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
                System.Drawing.Color backgroundColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
                System.Drawing.Color foregroundColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                
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
                
                // Line number styling in AvalonEdit is typically done via XAML resources
                // We've applied the main editor colors, which is the most important part
            }
            catch
            {
                // Line number margin styling may not be directly accessible
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
            if (start > end) { int temp = start; start = end; end = temp; }

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
            if (textEditor != null)
            {
                textEditor.ScrollToLine(textEditor.TextArea.Caret.Line);
            }
        }

        public void EmptyUndoBuffer()
        {
            if (textEditor?.Document?.UndoStack != null)
            {
                textEditor.Document.UndoStack.ClearAll();
            }
        }

        public void SetSelectionBackColor(bool useSelection, System.Drawing.Color color)
        {
            if (textEditor != null)
            {
                // AvalonEdit uses a different selection highlighting system
                // We can set the selection background via the highlighting system
                // For now, this is handled by the default selection highlighting
            }
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
                    var marker = new TextMarker(start, end - start);
                    marker.BackgroundColor = System.Windows.Media.Color.FromArgb(150, 128, 128, 128); // Semi-transparent gray
                    marker.ForegroundColor = System.Windows.Media.Color.FromArgb(200, 128, 128, 128); // Grayed out text
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
            private ICSharpCode.AvalonEdit.Document.TextDocument document;

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
            private ICSharpCode.AvalonEdit.Document.DocumentLine line;
            private ICSharpCode.AvalonEdit.Document.TextDocument document;

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
                // SearchPanel disabled - no cleanup needed
                // searchPanel?.Uninstall();
                // FoldingManager doesn't implement IDisposable, just clear reference
                foldingManager = null;
                // TextEditor doesn't implement IDisposable, just clear references
                textEditor = null;
                elementHost?.Dispose();
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

            int firstErrorOffset;
            var newFoldings = CreateNewFoldings(document, out firstErrorOffset);
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

