#region Imports

using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Drawing.Poison;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

#endregion

namespace T7CompilerGUI.Helpers
{
    /// <summary>
    /// Helper class for WPF integration with Poison theme
    /// Provides utilities for working with WPF controls in WinForms applications
    /// </summary>
    public static class PoisonWpfHelper
    {
        #region Visual Tree Helpers

        /// <summary>
        /// Finds a visual child of the specified type in the visual tree
        /// </summary>
        public static T FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate = null) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t && (predicate == null || predicate(t)))
                    return t;

                var childOfChild = FindVisualChild<T>(child, predicate);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        /// <summary>
        /// Finds all visual children of the specified type in the visual tree
        /// </summary>
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    yield return t;

                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

        /// <summary>
        /// Finds a visual parent of the specified type in the visual tree
        /// </summary>
        public static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;

            return FindVisualParent<T>(parentObject);
        }

        /// <summary>
        /// Finds a child by name in the visual tree
        /// </summary>
        public static T FindVisualChildByName<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            if (parent == null || string.IsNullOrEmpty(name)) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement fe && fe.Name == name && child is T t)
                    return t;

                var result = FindVisualChildByName<T>(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        #endregion

        #region Color Conversion

        /// <summary>
        /// Converts a WinForms Color to WPF Color
        /// </summary>
        public static System.Windows.Media.Color ToWpfColor(System.Drawing.Color winFormsColor)
        {
            return System.Windows.Media.Color.FromArgb(winFormsColor.A, winFormsColor.R, winFormsColor.G, winFormsColor.B);
        }

        /// <summary>
        /// Converts a WPF Color to WinForms Color
        /// </summary>
        public static System.Drawing.Color ToWinFormsColor(System.Windows.Media.Color wpfColor)
        {
            return System.Drawing.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
        }

        /// <summary>
        /// Converts a WinForms Color to WPF SolidColorBrush
        /// </summary>
        public static SolidColorBrush ToWpfBrush(System.Drawing.Color winFormsColor)
        {
            return new SolidColorBrush(ToWpfColor(winFormsColor));
        }

        /// <summary>
        /// Converts a WPF Color to WPF SolidColorBrush
        /// </summary>
        public static SolidColorBrush ToBrush(System.Windows.Media.Color wpfColor)
        {
            return new SolidColorBrush(wpfColor);
        }

        /// <summary>
        /// Gets a WPF color from Poison theme using a color getter function
        /// </summary>
        public static System.Windows.Media.Color GetThemeColor(PoisonStyleManager styleManager, Func<ThemeStyle, System.Drawing.Color> colorGetter)
        {
            if (styleManager == null) return Colors.White;
            var winFormsColor = colorGetter(styleManager.Theme);
            return ToWpfColor(winFormsColor);
        }

        #endregion

        #region Resource Dictionary Helpers

        /// <summary>
        /// Creates a WPF ResourceDictionary with Poison theme colors
        /// </summary>
        public static ResourceDictionary CreatePoisonResourceDictionary(PoisonStyleManager styleManager)
        {
            if (styleManager == null) return new ResourceDictionary();

            var backgroundColor = PoisonPaint.BackColor.Form(styleManager.Theme);
            var foregroundColor = PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
            var styleColor = PoisonPaint.GetStyleColor(styleManager.Style);

            var resources = new ResourceDictionary
            {
                ["PoisonBackgroundBrush"] = ToWpfBrush(backgroundColor),
                ["PoisonForegroundBrush"] = ToWpfBrush(foregroundColor),
                ["PoisonStyleColorBrush"] = ToWpfBrush(styleColor),
                ["PoisonBackgroundColor"] = ToWpfColor(backgroundColor),
                ["PoisonForegroundColor"] = ToWpfColor(foregroundColor),
                ["PoisonStyleColor"] = ToWpfColor(styleColor)
            };

            return resources;
        }

        /// <summary>
        /// Applies Poison theme resources to a WPF element
        /// </summary>
        public static void ApplyPoisonResources(FrameworkElement element, PoisonStyleManager styleManager)
        {
            if (element == null || styleManager == null) return;

            var resources = CreatePoisonResourceDictionary(styleManager);
            if (element.Resources == null)
            {
                element.Resources = new ResourceDictionary();
            }

            foreach (var key in resources.Keys)
            {
                element.Resources[key] = resources[key];
            }
        }

        /// <summary>
        /// Applies Poison theme resources recursively to a WPF element and its children
        /// </summary>
        public static void ApplyPoisonResourcesRecursive(DependencyObject element, PoisonStyleManager styleManager)
        {
            if (element == null || styleManager == null) return;

            if (element is FrameworkElement fe)
            {
                ApplyPoisonResources(fe, styleManager);
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                ApplyPoisonResourcesRecursive(child, styleManager);
            }
        }

        #endregion

        #region Scrollbar Styling

        /// <summary>
        /// Configures a WPF ScrollBar to match Poison theme with thumb-only display
        /// </summary>
        public static void StylePoisonScrollBar(ScrollBar scrollBar, PoisonStyleManager styleManager, 
            double thumbOpacity = 1.0, bool hideTrack = true, bool hideArrows = true)
        {
            if (scrollBar == null || styleManager == null) return;

            try
            {
                var backgroundColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                var styleColor = PoisonPaint.GetStyleColor(styleManager.Style);

                // Make scrollbar background transparent
                scrollBar.Background = System.Windows.Media.Brushes.Transparent;

                // Style the track
                var track = FindVisualChild<Track>(scrollBar);
                if (track != null)
                {
                    // Style thumb
                    if (track.Thumb != null)
                    {
                        track.Thumb.Visibility = Visibility.Visible;
                        track.Thumb.IsEnabled = true;
                        track.Thumb.Opacity = thumbOpacity;

                        var thumbBrush = ToWpfBrush(styleColor);
                        thumbBrush.Opacity = thumbOpacity;
                        track.Thumb.Background = thumbBrush;
                        track.Thumb.BorderBrush = thumbBrush;
                        track.Thumb.BorderThickness = new Thickness(0);
                    }

                    // Hide arrows
                    if (hideArrows)
                    {
                        if (track.DecreaseRepeatButton != null)
                        {
                            track.DecreaseRepeatButton.Visibility = Visibility.Collapsed;
                            track.DecreaseRepeatButton.IsHitTestVisible = false;
                        }
                        if (track.IncreaseRepeatButton != null)
                        {
                            track.IncreaseRepeatButton.Visibility = Visibility.Collapsed;
                            track.IncreaseRepeatButton.IsHitTestVisible = false;
                        }
                    }

                    // Hide track background
                    if (hideTrack)
                    {
                        foreach (var rect in FindVisualChildren<System.Windows.Shapes.Rectangle>(track))
                        {
                            var parent = VisualTreeHelper.GetParent(rect);
                            bool isPartOfThumb = false;

                            while (parent != null)
                            {
                                if (parent == track.Thumb)
                                {
                                    isPartOfThumb = true;
                                    break;
                                }
                                parent = VisualTreeHelper.GetParent(parent);
                            }

                            if (!isPartOfThumb)
                            {
                                rect.Fill = System.Windows.Media.Brushes.Transparent;
                                rect.Stroke = System.Windows.Media.Brushes.Transparent;
                                rect.Visibility = Visibility.Collapsed;
                                rect.IsHitTestVisible = false;
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
        /// Styles all scrollbars in a ScrollViewer with Poison theme
        /// </summary>
        public static void StylePoisonScrollViewer(ScrollViewer scrollViewer, PoisonStyleManager styleManager,
            double thumbOpacity = 1.0, bool hideTrack = true, bool hideArrows = true)
        {
            if (scrollViewer == null || styleManager == null) return;

            scrollViewer.Background = System.Windows.Media.Brushes.Transparent;

            var horizontalScrollBar = FindVisualChild<ScrollBar>(
                scrollViewer,
                sb => sb.Orientation == Orientation.Horizontal);
            var verticalScrollBar = FindVisualChild<ScrollBar>(
                scrollViewer,
                sb => sb.Orientation == Orientation.Vertical);

            if (horizontalScrollBar != null)
            {
                StylePoisonScrollBar(horizontalScrollBar, styleManager, thumbOpacity, hideTrack, hideArrows);
            }

            if (verticalScrollBar != null)
            {
                StylePoisonScrollBar(verticalScrollBar, styleManager, thumbOpacity, hideTrack, hideArrows);
            }

            // Hide corner control
            HideScrollViewerCorner(scrollViewer);
        }

        /// <summary>
        /// Hides the corner control in a ScrollViewer using multiple approaches
        /// The corner control is the small box where horizontal and vertical scrollbars meet
        /// </summary>
        public static void HideScrollViewerCorner(ScrollViewer scrollViewer)
        {
            if (scrollViewer == null) return;

            try
            {
                // Approach 1: Use template-based hiding (standard WPF approach)
                // PART_CornerControl is the named part in ScrollViewer's default template
                // First, ensure template is applied if it hasn't been yet
                if (scrollViewer.Template == null)
                {
                    // Apply default template if not already applied
                    scrollViewer.ApplyTemplate();
                }
                
                if (scrollViewer.Template != null)
                {
                    var cornerControl = scrollViewer.Template.FindName("PART_CornerControl", scrollViewer) as FrameworkElement;
                    if (cornerControl != null)
                    {
                        cornerControl.Visibility = Visibility.Collapsed;
                        cornerControl.IsHitTestVisible = false;
                        cornerControl.Opacity = 0;
                        cornerControl.Height = 0;
                        cornerControl.Width = 0;
                        cornerControl.Margin = new Thickness(0);
                        // Padding is only available on Control, not all FrameworkElements
                        if (cornerControl is System.Windows.Controls.Control control)
                        {
                            control.Padding = new Thickness(0);
                        }
                    }
                }
                
                // Approach 2: Also search visual tree for corner control (fallback)
                // Sometimes the template approach doesn't work if template wasn't applied yet
                var allElements = FindVisualChildren<FrameworkElement>(scrollViewer).ToList();
                foreach (var element in allElements)
                {
                    // Check if this element is the corner control by name or position
                    if (element.Name == "PART_CornerControl" || 
                        element.Name == "CornerControl" ||
                        (element.HorizontalAlignment == HorizontalAlignment.Right &&
                         element.VerticalAlignment == VerticalAlignment.Bottom &&
                         element is System.Windows.Controls.ContentControl))
                    {
                        element.Visibility = Visibility.Collapsed;
                        element.IsHitTestVisible = false;
                        element.Opacity = 0;
                        element.Height = 0;
                        element.Width = 0;
                        element.Margin = new Thickness(0);
                        // Padding is only available on Control, not all FrameworkElements
                        if (element is System.Windows.Controls.Control control)
                        {
                            control.Padding = new Thickness(0);
                        }
                    }
                }
                
                // Approach 3: Hide any Rectangle or Border elements in the corner region
                // The corner control might be rendered as a simple shape
                var rects = FindVisualChildren<System.Windows.Shapes.Rectangle>(scrollViewer).ToList();
                foreach (var rect in rects)
                {
                    // Check if rectangle is positioned in corner (right-bottom)
                    var parent = FindVisualParent<FrameworkElement>(rect);
                    if (parent != null)
                    {
                        var transform = rect.TransformToAncestor(scrollViewer);
                        var bounds = transform.TransformBounds(new System.Windows.Rect(0, 0, rect.ActualWidth, rect.ActualHeight));
                        
                        // If rectangle is in the bottom-right corner area, hide it
                        if (bounds.Right >= scrollViewer.ActualWidth - 20 && 
                            bounds.Bottom >= scrollViewer.ActualHeight - 20)
                        {
                            rect.Visibility = Visibility.Collapsed;
                            rect.Opacity = 0;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors - corner control hiding is not critical
            }
        }

        #endregion

        #region Dispatcher Helpers

        /// <summary>
        /// Safely invokes an action on the WPF dispatcher thread
        /// </summary>
        public static void InvokeOnDispatcher(DependencyObject element, System.Action action, DispatcherPriority priority = DispatcherPriority.Background)
        {
            if (element == null || action == null) return;

            var dispatcher = element.Dispatcher;
            if (dispatcher == null) return;

            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.BeginInvoke(priority, action);
            }
        }

        /// <summary>
        /// Safely invokes a function on the WPF dispatcher thread
        /// </summary>
        public static T InvokeOnDispatcher<T>(DependencyObject element, Func<T> func, DispatcherPriority priority = DispatcherPriority.Background)
        {
            if (element == null || func == null) return default(T);

            var dispatcher = element.Dispatcher;
            if (dispatcher == null) return default(T);

            if (dispatcher.CheckAccess())
            {
                return func();
            }
            else
            {
                return (T)dispatcher.Invoke(priority, func);
            }
        }

        #endregion

        #region ElementHost Helpers

        /// <summary>
        /// Configures an ElementHost to work seamlessly with Poison theme
        /// </summary>
        public static void ConfigureElementHost(ElementHost elementHost, PoisonStyleManager styleManager)
        {
            if (elementHost == null || styleManager == null) return;

            var backgroundColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
            elementHost.BackColor = backgroundColor;

            // Apply resources to the hosted WPF element
            if (elementHost.Child is FrameworkElement wpfElement)
            {
                ApplyPoisonResources(wpfElement, styleManager);
            }
        }

        /// <summary>
        /// Creates a fully configured ElementHost with Poison theme
        /// </summary>
        public static ElementHost CreateElementHost(FrameworkElement wpfChild, PoisonStyleManager styleManager)
        {
            if (wpfChild == null) return null;

            var elementHost = new ElementHost
            {
                Child = wpfChild,
                Dock = System.Windows.Forms.DockStyle.Fill
            };

            if (styleManager != null)
            {
                ConfigureElementHost(elementHost, styleManager);
            }

            return elementHost;
        }

        #endregion

        #region Theme Application Helpers

        /// <summary>
        /// Applies Poison theme to a WPF control
        /// </summary>
        public static void ApplyPoisonTheme(Control control, PoisonStyleManager styleManager)
        {
            if (control == null || styleManager == null) return;

            var backgroundColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
            var foregroundColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);

            control.Background = ToWpfBrush(backgroundColor);
            control.Foreground = ToWpfBrush(foregroundColor);

            ApplyPoisonResources(control, styleManager);
        }

        /// <summary>
        /// Applies Poison theme recursively to WPF controls
        /// </summary>
        public static void ApplyPoisonThemeRecursive(DependencyObject element, PoisonStyleManager styleManager)
        {
            if (element == null || styleManager == null) return;

            if (element is Control control)
            {
                ApplyPoisonTheme(control, styleManager);
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                ApplyPoisonThemeRecursive(child, styleManager);
            }
        }

        #endregion

        #region Event Conversion Helpers

        /// <summary>
        /// Converts WPF Key to WinForms Keys
        /// </summary>
        public static System.Windows.Forms.Keys ConvertWpfKey(System.Windows.Input.Key wpfKey)
        {
            if (wpfKey >= System.Windows.Input.Key.A && wpfKey <= System.Windows.Input.Key.Z)
                return (System.Windows.Forms.Keys)((int)System.Windows.Forms.Keys.A + (wpfKey - System.Windows.Input.Key.A));
            if (wpfKey >= System.Windows.Input.Key.D0 && wpfKey <= System.Windows.Input.Key.D9)
                return (System.Windows.Forms.Keys)((int)System.Windows.Forms.Keys.D0 + (wpfKey - System.Windows.Input.Key.D0));
            if (wpfKey >= System.Windows.Input.Key.NumPad0 && wpfKey <= System.Windows.Input.Key.NumPad9)
                return (System.Windows.Forms.Keys)((int)System.Windows.Forms.Keys.NumPad0 + (wpfKey - System.Windows.Input.Key.NumPad0));

            switch (wpfKey)
            {
                case System.Windows.Input.Key.Enter: return System.Windows.Forms.Keys.Enter;
                case System.Windows.Input.Key.Escape: return System.Windows.Forms.Keys.Escape;
                case System.Windows.Input.Key.Tab: return System.Windows.Forms.Keys.Tab;
                case System.Windows.Input.Key.Back: return System.Windows.Forms.Keys.Back;
                case System.Windows.Input.Key.Delete: return System.Windows.Forms.Keys.Delete;
                case System.Windows.Input.Key.Insert: return System.Windows.Forms.Keys.Insert;
                case System.Windows.Input.Key.Home: return System.Windows.Forms.Keys.Home;
                case System.Windows.Input.Key.End: return System.Windows.Forms.Keys.End;
                case System.Windows.Input.Key.PageUp: return System.Windows.Forms.Keys.PageUp;
                case System.Windows.Input.Key.PageDown: return System.Windows.Forms.Keys.PageDown;
                case System.Windows.Input.Key.Left: return System.Windows.Forms.Keys.Left;
                case System.Windows.Input.Key.Right: return System.Windows.Forms.Keys.Right;
                case System.Windows.Input.Key.Up: return System.Windows.Forms.Keys.Up;
                case System.Windows.Input.Key.Down: return System.Windows.Forms.Keys.Down;
                case System.Windows.Input.Key.F1: return System.Windows.Forms.Keys.F1;
                case System.Windows.Input.Key.F2: return System.Windows.Forms.Keys.F2;
                case System.Windows.Input.Key.F3: return System.Windows.Forms.Keys.F3;
                case System.Windows.Input.Key.F4: return System.Windows.Forms.Keys.F4;
                case System.Windows.Input.Key.F5: return System.Windows.Forms.Keys.F5;
                case System.Windows.Input.Key.F6: return System.Windows.Forms.Keys.F6;
                case System.Windows.Input.Key.F7: return System.Windows.Forms.Keys.F7;
                case System.Windows.Input.Key.F8: return System.Windows.Forms.Keys.F8;
                case System.Windows.Input.Key.F9: return System.Windows.Forms.Keys.F9;
                case System.Windows.Input.Key.F10: return System.Windows.Forms.Keys.F10;
                case System.Windows.Input.Key.F11: return System.Windows.Forms.Keys.F11;
                case System.Windows.Input.Key.F12: return System.Windows.Forms.Keys.F12;
                default: return System.Windows.Forms.Keys.None;
            }
        }

        /// <summary>
        /// Converts WPF ModifierKeys to WinForms Keys
        /// </summary>
        public static System.Windows.Forms.Keys ConvertWpfModifiers(System.Windows.Input.ModifierKeys modifiers)
        {
            System.Windows.Forms.Keys result = System.Windows.Forms.Keys.None;
            if ((modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
                result |= System.Windows.Forms.Keys.Control;
            if ((modifiers & System.Windows.Input.ModifierKeys.Shift) != 0)
                result |= System.Windows.Forms.Keys.Shift;
            if ((modifiers & System.Windows.Input.ModifierKeys.Alt) != 0)
                result |= System.Windows.Forms.Keys.Alt;
            return result;
        }

        /// <summary>
        /// Converts WPF MouseButton to WinForms MouseButtons
        /// </summary>
        public static System.Windows.Forms.MouseButtons ConvertWpfMouseButton(System.Windows.Input.MouseButton button)
        {
            switch (button)
            {
                case System.Windows.Input.MouseButton.Left: return System.Windows.Forms.MouseButtons.Left;
                case System.Windows.Input.MouseButton.Right: return System.Windows.Forms.MouseButtons.Right;
                case System.Windows.Input.MouseButton.Middle: return System.Windows.Forms.MouseButtons.Middle;
                default: return System.Windows.Forms.MouseButtons.None;
            }
        }

        #endregion

        #region WPF Control Creation Helpers

        /// <summary>
        /// Creates a fully configured ElementHost with a WPF control and Poison theme
        /// </summary>
        public static ElementHost CreateThemedElementHost(FrameworkElement wpfControl, PoisonStyleManager styleManager)
        {
            if (wpfControl == null) return null;

            var elementHost = CreateElementHost(wpfControl, styleManager);
            
            if (styleManager != null)
            {
                // Apply theme recursively to the WPF control
                ApplyPoisonThemeRecursive(wpfControl, styleManager);
                
                // If it's a ScrollViewer, style it
                if (wpfControl is ScrollViewer sv)
                {
                    StylePoisonScrollViewer(sv, styleManager);
                }
                // If it contains ScrollViewers, style them too
                else
                {
                    foreach (var scrollViewer in FindVisualChildren<ScrollViewer>(wpfControl))
                    {
                        StylePoisonScrollViewer(scrollViewer, styleManager);
                    }
                }
            }

            return elementHost;
        }

        /// <summary>
        /// Applies Poison theme to an existing ElementHost and its WPF child
        /// </summary>
        public static void ApplyPoisonThemeToElementHost(ElementHost elementHost, PoisonStyleManager styleManager)
        {
            if (elementHost == null || styleManager == null) return;

            ConfigureElementHost(elementHost, styleManager);

            if (elementHost.Child is FrameworkElement wpfElement)
            {
                ApplyPoisonThemeRecursive(wpfElement, styleManager);

                // Style any ScrollViewers
                foreach (var scrollViewer in FindVisualChildren<ScrollViewer>(wpfElement))
                {
                    StylePoisonScrollViewer(scrollViewer, styleManager);
                }
            }
        }

        /// <summary>
        /// Creates a WPF ScrollViewer with Poison-themed scrollbars
        /// </summary>
        public static ScrollViewer CreatePoisonScrollViewer(UIElement content, PoisonStyleManager styleManager,
            double thumbOpacity = 1.0, bool hideTrack = true, bool hideArrows = true)
        {
            var scrollViewer = new ScrollViewer
            {
                Content = content,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            if (styleManager != null)
            {
                ApplyPoisonTheme(scrollViewer, styleManager);

                // Style scrollbars when loaded
                scrollViewer.Loaded += (s, e) =>
                {
                    StylePoisonScrollViewer(scrollViewer, styleManager, thumbOpacity, hideTrack, hideArrows);
                };
            }

            return scrollViewer;
        }

        /// <summary>
        /// Converts WinForms colors to WPF brushes for common use cases
        /// </summary>
        public static class PoisonBrushes
        {
            /// <summary>
            /// Gets a brush for the form background color
            /// </summary>
            public static SolidColorBrush FormBackground(PoisonStyleManager styleManager)
            {
                if (styleManager == null) return new SolidColorBrush(Colors.Black);
                var color = PoisonPaint.BackColor.Form(styleManager.Theme);
                return ToWpfBrush(color);
            }

            /// <summary>
            /// Gets a brush for label foreground color
            /// </summary>
            public static SolidColorBrush LabelForeground(PoisonStyleManager styleManager)
            {
                if (styleManager == null) return new SolidColorBrush(Colors.White);
                var color = PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                return ToWpfBrush(color);
            }

            /// <summary>
            /// Gets a brush for the style color (accent color)
            /// </summary>
            public static SolidColorBrush StyleColor(PoisonStyleManager styleManager)
            {
                if (styleManager == null) return new SolidColorBrush(Colors.Blue);
                var color = PoisonPaint.GetStyleColor(styleManager.Style);
                return ToWpfBrush(color);
            }

            /// <summary>
            /// Gets a brush for button hover background
            /// </summary>
            public static SolidColorBrush ButtonHoverBackground(PoisonStyleManager styleManager)
            {
                if (styleManager == null) return new SolidColorBrush(Colors.Gray);
                var color = PoisonPaint.BackColor.Button.Hover(styleManager.Theme);
                return ToWpfBrush(color);
            }

            /// <summary>
            /// Gets a brush for button pressed background
            /// </summary>
            public static SolidColorBrush ButtonPressedBackground(PoisonStyleManager styleManager)
            {
                if (styleManager == null) return new SolidColorBrush(Colors.DarkGray);
                var color = PoisonPaint.BackColor.Button.Press(styleManager.Theme);
                return ToWpfBrush(color);
            }
        }

        #endregion
    }
}

