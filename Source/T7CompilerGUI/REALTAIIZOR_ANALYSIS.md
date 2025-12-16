# ReaLTaiizor Source Analysis - Available Helper Methods and Classes

This document provides a comprehensive overview of all helper methods, classes, and utilities available in the ReaLTaiizor library that should be used in the T7CompilerGUI project.

## Namespace: `ReaLTaiizor.Extension.Poison`

### `PoisonControlHelper` - Main Control Helper Class

#### Style Manager Application
- **`ApplyStyleManager(Control control, PoisonStyleManager styleManager)`**
  - Recursively applies StyleManager to control and all children
  - Sets Theme, Style, and UseStyleColors automatically
  - **Usage**: Replace all manual `StyleManager =`, `Theme =`, `Style =` assignments

- **`ApplyStyleManagerToContainer(Control container, PoisonStyleManager styleManager, bool useStyleColors = true)`**
  - Convenience method combining ApplyStyleManager and EnableStyleColors

- **`ApplyStyleManagerToControls(PoisonStyleManager styleManager, params Control[] controls)`**
  - Batch applies StyleManager to multiple controls

#### Finding Controls
- **`FindPoisonControls(Control container)`** → `List<IPoisonControl>`
  - Recursively finds all Poison controls in a container

- **`FindControlsOfType<T>(Control container)`** → `List<T>`
  - Finds all controls of a specific type recursively

#### UseStyleColors Management
- **`EnableStyleColors(Control container, bool value = true)`**
  - Recursively enables/disables UseStyleColors for all Poison controls
  - Uses direct property access for performance (no reflection for known types)

- **`EnableStyleColorsForControls(bool value, params Control[] controls)`**
  - Batch enables UseStyleColors for multiple controls

#### Theme and Style Updates
- **`UpdateThemeAndStyle(Control container, ThemeStyle theme, ColorStyle style)`**
  - Updates theme and style for all Poison controls
  - Calls Invalidate() automatically

- **`UpdateThemeAndStyleWithRefresh(Control container, ThemeStyle theme, ColorStyle style)`**
  - Updates theme/style and refreshes all controls

#### Button Setup and Effects
- **`SetupButton(PoisonButton button, PoisonStyleManager styleManager = null, bool useStyleColors = true, bool enableCursorHand = true)`**
  - Sets up button with StyleManager, UseStyleColors, UseSelectable
  - Adds cursor handling (hand cursor on hover)

- **`SetupButtonEffects(PoisonButton button, PoisonStyleManager styleManager = null, bool enableCursorHand = true)`**
  - Alias for SetupButton

- **`SetupButtonEffectsRecursive(Control container, PoisonStyleManager styleManager = null, bool enableCursorHand = true)`**
  - Recursively sets up button effects for all buttons in container

- **`SetupAllButtonEffectsRecursive(Control container, PoisonStyleManager styleManager = null, bool enableCursorHand = true)`**
  - Sets up effects for both PoisonButton and PoisonDropDownButton recursively

- **`SetupDropDownButtonEffects(PoisonDropDownButton button, PoisonStyleManager styleManager = null, bool enableCursorHand = true)`**
  - Sets up dropdown button with effects

#### Control Creation Methods
All Create* methods return fully configured controls with StyleManager, Theme, Style, and UseStyleColors set:

- **`CreateButton(string text, PoisonStyleManager styleManager, EventHandler clickHandler = null, bool useStyleColors = true)`** → `PoisonButton`
- **`CreateDropDownButton(string text, ContextMenuStrip menu, PoisonStyleManager styleManager, bool behaveLikeComboBox = true, bool useStyleColors = true)`** → `PoisonDropDownButton`
- **`CreateLabel(string text, PoisonStyleManager styleManager, bool useStyleColors = true, ContentAlignment textAlign = ContentAlignment.MiddleLeft)`** → `PoisonLabel`
- **`CreateTextBox(string text = "", PoisonStyleManager styleManager = null, bool useStyleColors = true, bool useSelectable = true)`** → `PoisonTextBox`
- **`CreateComboBox(PoisonStyleManager styleManager = null, bool useStyleColors = true)`** → `PoisonComboBox`
- **`CreateCheckBox(string text, bool @checked = false, PoisonStyleManager styleManager = null, bool useStyleColors = true, EventHandler checkedChanged = null)`** → `PoisonCheckBox`
- **`CreateRadioButton(string text, bool @checked = false, PoisonStyleManager styleManager = null, bool useStyleColors = true, EventHandler checkedChanged = null)`** → `PoisonRadioButton`
- **`CreateProgressBar(int minimum = 0, int maximum = 100, int value = 0, PoisonStyleManager styleManager = null, bool useStyleColors = true)`** → `PoisonProgressBar`
- **`CreateToggle(string text, bool @checked = false, PoisonStyleManager styleManager = null, bool useStyleColors = true, EventHandler checkedChanged = null)`** → `PoisonToggle`
- **`CreateStyledPanel(PoisonStyleManager styleManager, bool useStyleColors = true)`** → `PoisonPanel`
- **`CreateUserControl(PoisonStyleManager styleManager = null, bool useStyleColors = true)`** → `PoisonUserControl`
- **`CreateTabControl(PoisonStyleManager styleManager, bool useStyleColors = true)`** → `PoisonTabControl`
- **`CreateTabPage(string text, PoisonStyleManager styleManager, bool useStyleColors = true)`** → `PoisonTabPage`
- **`CreateContainer(PoisonStyleManager styleManager, params Control[] controls)`** → `PoisonPanel`
  - Creates panel and adds controls to it

#### Scrollbar Configuration
- **`ConfigureScrollbars(PoisonPanel panel, bool showVertical = true, bool showHorizontal = false, bool verticalThumbOnly = false, bool horizontalThumbOnly = false, bool verticalInvisible = false, bool horizontalInvisible = false)`**
  - Configures scrollbar visibility and appearance for a panel

- **`SetInvisibleScrollbars(PoisonPanel panel)`**
  - Makes scrollbars invisible but functional

- **`SetThumbOnlyScrollbars(PoisonPanel panel)`**
  - Shows only the draggable thumb

- **`ConfigureTabPageScrollbars(PoisonTabPage tabPage, ...)`**
  - Same options as ConfigureScrollbars but for tab pages

- **`SetTabPageInvisibleScrollbars(PoisonTabPage tabPage)`**
  - Makes tab page scrollbars invisible

- **`ConfigureScrollbarsRecursive(Control container, Func<PoisonPanel, bool> panelConfig = null, Func<PoisonTabPage, bool> tabPageConfig = null)`**
  - Recursively configures scrollbars for all panels and tab pages

- **`CreatePanelWithScrollbars(PoisonStyleManager styleManager, bool useStyleColors = true, bool autoScroll = true, bool verticalInvisible = false, bool horizontalInvisible = false, bool verticalThumbOnly = false, bool horizontalThumbOnly = false)`** → `PoisonPanel`
  - Creates panel with scrollbar configuration

- **`ConfigureScrollbarsBatch(bool verticalInvisible = false, bool horizontalInvisible = false, bool verticalThumbOnly = false, bool horizontalThumbOnly = false, params PoisonPanel[] panels)`**
  - Batch configures scrollbars for multiple panels

#### Button State Management
- **`ResetButtonState(Control control)`**
  - Resets hover/pressed state of a button (uses reflection for compatibility)

- **`ResetAllButtonStates(Control container)`**
  - Recursively resets all button states in a container

#### Button Click Handler Helpers
- **`CreateClickHandlerWithReset(EventHandler originalHandler)`** → `EventHandler`
  - Wraps handler to automatically reset button state after click

- **`WrapButtonClick(System.Action action)`** → `EventHandler`
  - Wraps action to automatically reset button state

- **`WrapButtonClick(Action<object> action)`** → `EventHandler`
  - Wraps action with sender parameter

#### Control Setup Presets
- **`ApplySetupPreset(Control control, PoisonStyleManager styleManager, bool useStyleColors = true, bool useSelectable = true, bool enableButtonEffects = true)`**
  - Applies common setup preset (StyleManager, UseStyleColors, button effects)

- **`SetupControl(Control control, PoisonStyleManager styleManager, bool useStyleColors = true, bool recursive = true)`**
  - Sets up control with StyleManager and applies recursively

- **`SetupControlsBatch(PoisonStyleManager styleManager, bool useStyleColors = true, params Control[] controls)`**
  - Batch setup for multiple controls

#### Control Registration (Automatic Updates)
- **`RegisterControl(Control control, PoisonStyleManager styleManager)`**
  - Registers control for automatic StyleManager updates
  - Automatically cleaned up when control is disposed

- **`UpdateRegisteredControls(PoisonStyleManager styleManager)`**
  - Updates all registered controls with new StyleManager

#### Layout Container
- **`CreateLayoutContainer(PoisonStyleManager styleManager, DockStyle dock = DockStyle.None, bool useStyleColors = true)`** → `PoisonPanel`
  - Creates panel with automatic StyleManager propagation for child controls
  - Child controls added later automatically inherit StyleManager

#### Utility Methods
- **`GetEffectiveTheme(IPoisonControl control)`** → `ThemeStyle`
  - Gets effective theme (checks StyleManager first, then control property)

- **`GetEffectiveStyle(IPoisonControl control)`** → `ColorStyle`
  - Gets effective style (checks StyleManager first, then control property)

- **`InitializeForm(Form form, PoisonStyleManager styleManager, bool useStyleColors = true)`**
  - Initializes form with StyleManager (applies to form and all controls)

- **`RefreshAllControls(Control container)`**
  - Invalidates all Poison controls in container

- **`InvokeOnUIThread(Control control, System.Action action)`**
  - Safely invokes action on UI thread

- **`InvokeOnUIThread<T>(Control control, Func<T> func)`** → `T`
  - Safely invokes function on UI thread and returns result

#### Fluent Setup API
- **`Setup(PoisonStyleManager styleManager)`** → `ControlSetup`
  - Creates fluent-style control setup configuration
  - Usage: `PoisonControlHelper.Setup(styleManager).ApplyTo(control)`

---

### `PoisonFormHelper` - Form Lifecycle Management

#### Form Initialization
- **`InitializeForm(PoisonForm form, PoisonStyleManager styleManager = null)`**
  - Standardized form initialization:
    - Sets StyleManager, Theme, Style on form
    - Applies StyleManager to all controls recursively
    - Sets up button effects recursively
    - Attempts to load form icon via FormIconHelper (if available)
  - **Usage**: Replace manual form initialization code

- **`InitializeFormWithTracking(PoisonForm form, PoisonStyleManager styleManager = null)`** → `ResourceTracker`
  - Same as InitializeForm but returns ResourceTracker for resource management

#### Resource Management
- **`ResourceTracker`** class
  - Tracks IDisposable resources for automatic cleanup
  - **`Track<T>(T resource)`** → `T` - Registers resource
  - **`Track(params IDisposable[] resourcesToTrack)`** - Registers multiple resources
  - **`Untrack(IDisposable resource)`** → `bool` - Removes from tracking
  - **`Dispose()`** - Disposes all tracked resources in reverse order

- **`GetResourceTracker(Form form)`** → `ResourceTracker`
  - Gets or creates ResourceTracker for a form
  - Automatically disposes when form is disposed

#### Event Handler Tracking
- **`EventHandlerTracker`** class
  - Tracks event handlers to prevent memory leaks
  - **`Track(Delegate handler)`** - Registers handler
  - **`Clear()`** - Clears tracked handlers
  - **`GetHandlers()`** → `List<Delegate>` - Gets all tracked handlers
  - **`Dispose()`** - Clears handlers

- **`GetEventHandlerTracker(Form form)`** → `EventHandlerTracker`
  - Gets or creates EventHandlerTracker for a form

#### Timer Management
- **`CreateTrackedTimer(Form form, int interval, EventHandler tickHandler)`** → `System.Windows.Forms.Timer`
  - Creates and tracks a Timer that will be automatically disposed
  - Timer is added to ResourceTracker

- **`DisposeTimer(System.Windows.Forms.Timer timer)`**
  - Safely stops and disposes a timer

#### Form Cleanup
- **`CleanupForm(Form form)`**
  - Standardized form cleanup
  - Disposes all tracked resources and event handlers

- **`HandleFormClosing(Form form, FormClosingEventArgs e, System.Action<bool> onClosing = null)`**
  - Safe form closing pattern with resource cleanup

#### Validation Helpers
- **`ValidateControls(params Control[] controls)`** → `bool`
  - Validates that required controls are not null or disposed

- **`ValidateControlsAccessible(params Control[] controls)`** → `bool`
  - Validates controls exist, are accessible, and visible

#### Error Handling
- **`SafeExecute(System.Action action, System.Action<Exception> onError = null)`** → `bool`
  - Safely executes action with error handling

- **`SafeExecute<T>(Func<T> func, T defaultValue = default(T), System.Action<Exception> onError = null)`** → `T`
  - Safely executes function with error handling

- **`SafeInvoke(Control control, System.Action action, System.Action<Exception> onError = null)`**
  - Safely invokes on UI thread with error handling

#### Form Utilities
- **`CenterForm(Form form, Form parent = null)`**
  - Centers form on parent or screen

- **`SetupAsDialog(PoisonForm form, PoisonStyleManager styleManager = null)`**
  - Sets form properties for modal dialog display
  - Calls InitializeForm and sets FormBorderStyle, MaximizeBox, MinimizeBox

---

## Namespace: `ReaLTaiizor.Drawing.Poison`

### `PoisonPaint` - Color and Drawing Utilities

#### Color Classes
- **`BorderColor`** - Border colors for different control states
  - `Form(ThemeStyle theme)` → `Color`
  - `Button.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`
  - `CheckBox.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`
  - `ComboBox.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`
  - `ProgressBar.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`
  - `TabControl.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`

- **`BackColor`** - Background colors
  - `Form(ThemeStyle theme)` → `Color`
  - `Button.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`
  - `TrackBar.Thumb.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`
  - `TrackBar.Bar.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`
  - `ScrollBar.Thumb.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`
  - `ScrollBar.Bar.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`
  - `ProgressBar.Bar.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`

- **`ForeColor`** - Foreground/text colors
  - `Button.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`
  - `Title(ThemeStyle theme)` → `Color`
  - `Tile.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`
  - `Link.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`
  - `Label.Normal/Disabled(ThemeStyle theme)` → `Color`
  - `CheckBox.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`
  - `ComboBox.Normal/Hover/Press/Disabled(ThemeStyle theme)` → `Color`
  - `ProgressBar.Normal/Disabled(ThemeStyle theme)` → `Color`
  - `TabControl.Normal/Disabled(ThemeStyle theme)` → `Color`

#### Helper Methods
- **`GetStyleColor(ColorStyle style)`** → `Color`
  - Gets color for a ColorStyle (supports Rainbow via callback)

- **`GetStyleBrush(ColorStyle style)`** → `SolidBrush`
  - Gets SolidBrush for a ColorStyle (supports Rainbow via callback)

- **`GetStylePen(ColorStyle style)`** → `Pen`
  - Gets Pen for a ColorStyle (supports Rainbow via callback)

- **`GetLuminance(Color color)`** → `double`
  - Calculates relative luminance (0.0 = darkest, 1.0 = brightest)

- **`GetContrastRatio(Color color1, Color color2)`** → `double`
  - Calculates contrast ratio (1.0 = no contrast, 21.0 = maximum)
  - WCAG AA requires 4.5:1 for normal text, 3:1 for large text

- **`GetContrastingTextColor(Color backgroundColor)`** → `Color`
  - Returns white for dark backgrounds, black for light backgrounds

- **`LightenColor(Color color, double amount)`** → `Color`
  - Lightens color by percentage (0.0 = no change, 1.0 = white)

- **`DarkenColor(Color color, double amount)`** → `Color`
  - Darkens color by percentage (0.0 = no change, 1.0 = black)

- **`BlendColors(Color color1, Color color2, double ratio)`** → `Color`
  - Blends two colors (0.0 = fully color1, 1.0 = fully color2)

- **`GetStringFormat(ContentAlignment textAlign)`** → `StringFormat`
  - Creates StringFormat for text alignment

- **`GetTextFormatFlags(ContentAlignment textAlign, bool WrapToLine = false)`** → `TextFormatFlags`
  - Creates TextFormatFlags for text alignment

#### Rainbow Color Support
- **`GetRainbowColor`** → `Func<Color>`
  - Callback function to get current rainbow color
  - Set this to return animated rainbow color when Rainbow style is active
  - Used by GetStyleColor, GetStyleBrush, GetStylePen

---

## Namespace: `ReaLTaiizor.Extension.Poison`

### `PoisonBrushes` - SolidBrush Utilities
- Static properties returning SolidBrush for each ColorStyle:
  - `Black`, `White`, `Silver`, `Blue`, `Green`, `Lime`, `Teal`, `Orange`, `Brown`, `Pink`, `Magenta`, `Purple`, `Red`, `Yellow`, `Custom`
- Brushes are cached and cloned when accessed

### `PoisonPens` - Pen Utilities
- Static properties returning Pen for each ColorStyle:
  - `Black`, `White`, `Silver`, `Blue`, `Green`, `Lime`, `Teal`, `Orange`, `Brown`, `Pink`, `Magenta`, `Purple`, `Red`, `Yellow`
- Pens are cached and cloned when accessed

### `PoisonFonts` - Font Utilities
- **`DefaultLight(float size)`** → `Font` - Segoe UI Light
- **`Default(float size)`** → `Font` - Segoe UI Regular
- **`DefaultBold(float size)`** → `Font` - Segoe UI Bold
- **`DefaultItalic(float size)`** → `Font` - Segoe UI Italic
- **`Title`** → `Font` - DefaultLight(24f)
- **`Subtitle`** → `Font` - Default(14f)
- **`TileCount`** → `Font` - Default(44f)

Control-specific font methods (all take Size and Weight enums):
- `Tile(PoisonTileTextSize, PoisonTileTextWeight)` → `Font`
- `LinkLabel(PoisonLinkLabelSize, PoisonLinkLabelWeight)` → `Font`
- `ComboBox(PoisonComboBoxSize, PoisonComboBoxWeight)` → `Font`
- `DateTime(PoisonDateTimeSize, PoisonDateTimeWeight)` → `Font`
- `Label(PoisonLabelSize, PoisonLabelWeight)` → `Font`
- `TextBox(PoisonTextBoxSize, PoisonTextBoxWeight)` → `Font`
- `ProgressBar(PoisonProgressBarSize, PoisonProgressBarWeight)` → `Font`
- `TabControl(PoisonTabControlSize, PoisonTabControlWeight)` → `Font`
- `CheckBox(PoisonCheckBoxSize, PoisonCheckBoxWeight)` → `Font`
- `WaterMark(PoisonLabelSize, PoisonWaterMarkWeight)` → `Font`
- `Button(PoisonButtonSize, PoisonButtonWeight)` → `Font`

### `PoisonImage` - Image Utilities
- **`ResizeImage(Image imgToResize, Rectangle maxOffset)`** → `Image`
  - Resizes image to fit within maxOffset while maintaining aspect ratio

---

## Namespace: `ReaLTaiizor.Helper`

### `PoisonDataGridHelper` - DataGridView Integration
Helper class for integrating PoisonScrollBar with DataGridView:
- Constructor: `PoisonDataGridHelper(PoisonScrollBar scrollbar, DataGridView grid, bool vertical = true)`
- **`UpdateScrollbar()`** - Updates scrollbar to match grid state
- **`VisibleVerticalScroll()`** → `bool` - Checks if vertical scrollbar should be visible
- **`VisibleHorizontalScroll()`** → `bool` - Checks if horizontal scrollbar should be visible

---

## Recommended Usage Patterns

### Form Initialization
```csharp
// Instead of manual initialization:
public MyForm(PoisonStyleManager styleManager)
{
    InitializeComponent();
    this.StyleManager = styleManager;
    this.Theme = styleManager.Theme;
    this.Style = styleManager.Style;
    // ... manual control setup
}

// Use:
public MyForm(PoisonStyleManager styleManager)
{
    InitializeComponent();
    PoisonFormHelper.InitializeForm(this, styleManager);
}
```

### Control Creation
```csharp
// Instead of:
var button = new PoisonButton
{
    Text = "Click Me",
    StyleManager = styleManager,
    Theme = styleManager.Theme,
    Style = styleManager.Style,
    UseStyleColors = true,
    UseSelectable = true
};

// Use:
var button = PoisonControlHelper.CreateButton("Click Me", styleManager);
```

### Applying StyleManager
```csharp
// Instead of:
control.StyleManager = styleManager;
control.Theme = styleManager.Theme;
control.Style = styleManager.Style;
if (control is PoisonButton btn) btn.UseStyleColors = true;
// ... repeat for all controls

// Use:
PoisonControlHelper.ApplyStyleManager(container, styleManager);
```

### Color Access
```csharp
// Instead of hardcoded colors:
button.BackColor = Color.FromArgb(34, 34, 34);
label.ForeColor = Color.FromArgb(170, 170, 170);

// Use:
var theme = styleManager?.Theme ?? ThemeStyle.Dark;
button.BackColor = PoisonPaint.BackColor.Button.Normal(theme);
label.ForeColor = PoisonPaint.ForeColor.Label.Normal(theme);
```

### Scrollbar Configuration
```csharp
// Instead of manual configuration:
panel.AutoScroll = true;
panel.VerticalScrollbar = true;
panel.VerticalScrollbarInvisible = true;
// ... etc

// Use:
PoisonControlHelper.SetInvisibleScrollbars(panel);
// or
PoisonControlHelper.ConfigureScrollbars(panel, 
    showVertical: true, 
    verticalInvisible: true);
```

### Resource Management
```csharp
// Track resources for automatic cleanup:
var tracker = PoisonFormHelper.GetResourceTracker(this);
var timer = PoisonFormHelper.CreateTrackedTimer(this, 1000, OnTick);
// Timer will be automatically disposed when form is disposed
```

---

## Summary of What We Should Use

1. **Replace all manual StyleManager/Theme/Style assignments** with `PoisonControlHelper.ApplyStyleManager()`
2. **Use `PoisonFormHelper.InitializeForm()`** for all form initialization
3. **Use `PoisonControlHelper.Create*()` methods** for dynamic control creation
4. **Use `PoisonPaint`** for all color access (no hardcoded colors)
5. **Use scrollbar configuration helpers** instead of manual property setting
6. **Use `PoisonFormHelper` resource tracking** for timers and other IDisposable resources
7. **Use button setup helpers** for consistent button behavior
8. **Use batch operations** when setting up multiple controls

