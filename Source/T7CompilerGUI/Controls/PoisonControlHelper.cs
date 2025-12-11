using System;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using ReaLTaiizor.Controls;

namespace T7CompilerGUI.Controls
{
    /// <summary>
    /// Centralized utility class for managing Poison control states and behaviors
    /// </summary>
    public static class PoisonControlHelper
    {
        /// <summary>
        /// Resets the hover and pressed state of a Poison button or dropdown button
        /// </summary>
        /// <param name="sender">The control to reset (PoisonButton or PoisonDropDownButton)</param>
        public static void ResetButtonState(object sender)
        {
            // Handle PoisonDropDownButton - uses PushButtonState enum, not isHovered/isPressed
            if (sender is PoisonDropDownButton dropDown)
            {
                ResetDropDownButtonState(dropDown);
            }
            // Handle PoisonButton - uses isHovered/isPressed fields
            else if (sender is PoisonButton btn)
            {
                ResetButtonStateInternal(btn);
            }
        }
        
        /// <summary>
        /// Resets PoisonDropDownButton state using the State property (PushButtonState enum)
        /// </summary>
        private static void ResetDropDownButtonState(PoisonDropDownButton dropDown)
        {
            if (dropDown == null) return;
            
            // PoisonDropDownButton uses a State property with PushButtonState enum
            // We need to call SetButtonDrawState() or set State to Normal
            // Use reflection to access the private SetButtonDrawState method
            var setButtonDrawStateMethod = dropDown.GetType().GetMethod("SetButtonDrawState", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (setButtonDrawStateMethod != null)
            {
                // Call SetButtonDrawState() which properly resets the state
                setButtonDrawStateMethod.Invoke(dropDown, null);
            }
            else
            {
                // Fallback: Try to set State property directly to Normal
                var stateProperty = dropDown.GetType().GetProperty("State", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (stateProperty != null && stateProperty.CanWrite)
                {
                    // PushButtonState.Normal = 1
                    stateProperty.SetValue(dropDown, PushButtonState.Normal);
                    dropDown.Invalidate();
                }
            }
        }
        
        /// <summary>
        /// Internal method to reset button state using reflection (for PoisonButton)
        /// </summary>
        private static void ResetButtonStateInternal(Control control)
        {
            if (control == null) return;
            
            var isHoveredField = control.GetType().GetField("isHovered", BindingFlags.NonPublic | BindingFlags.Instance);
            var isPressedField = control.GetType().GetField("isPressed", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (isHoveredField != null)
                isHoveredField.SetValue(control, false);
            if (isPressedField != null)
                isPressedField.SetValue(control, false);
            
            control.Invalidate();
        }
        
        /// <summary>
        /// Sets up proper hover and pressed effects for a PoisonButton
        /// </summary>
        /// <param name="button">The button to set up</param>
        public static void SetupButtonEffects(PoisonButton button)
        {
            if (button == null) return;
            
            // Get reflection info for internal state fields
            var isHoveredField = button.GetType().GetField("isHovered", BindingFlags.NonPublic | BindingFlags.Instance);
            var isPressedField = button.GetType().GetField("isPressed", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Wire up mouse events to ensure proper state reset
            // MouseUp fires after Click, so reset here to ensure state is cleared
            button.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    // Reset both hover and pressed states on mouse up
                    if (isHoveredField != null)
                        isHoveredField.SetValue(button, false);
                    if (isPressedField != null)
                        isPressedField.SetValue(button, false);
                    button.Invalidate();
                }
            };
            
            button.MouseLeave += (s, e) =>
            {
                if (isHoveredField != null)
                    isHoveredField.SetValue(button, false);
                if (isPressedField != null)
                    isPressedField.SetValue(button, false);
                button.Invalidate();
            };
            
            // Also reset on Click event to ensure state is cleared immediately
            button.Click += (s, e) =>
            {
                // Reset state immediately
                if (isHoveredField != null)
                    isHoveredField.SetValue(button, false);
                if (isPressedField != null)
                    isPressedField.SetValue(button, false);
                button.Invalidate();
            };
            
            // Add hover effect handlers
            button.MouseEnter += (s, e) => 
            {
                if (button.Enabled)
                {
                    button.Cursor = Cursors.Hand;
                    button.Invalidate();
                }
            };
        }
        
        /// <summary>
        /// Recursively sets up button effects for all PoisonButtons in a container
        /// </summary>
        /// <param name="container">The container control to search</param>
        public static void SetupButtonEffectsRecursive(Control container)
        {
            if (container == null) return;
            
            foreach (Control ctrl in container.Controls)
            {
                if (ctrl is PoisonButton btn)
                {
                    SetupButtonEffects(btn);
                }
                
                if (ctrl.HasChildren)
                {
                    SetupButtonEffectsRecursive(ctrl);
                }
            }
        }
        
        /// <summary>
        /// Sets up button effects for a PoisonDropDownButton
        /// </summary>
        /// <param name="dropDown">The dropdown button to set up</param>
        public static void SetupDropDownButtonEffects(PoisonDropDownButton dropDown)
        {
            if (dropDown == null) return;
            
            // PoisonDropDownButton uses PushButtonState enum via State property, not isHovered/isPressed
            // Get the SetButtonDrawState method to properly reset state
            var setButtonDrawStateMethod = dropDown.GetType().GetMethod("SetButtonDrawState", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Wire up mouse events to ensure proper state reset
            // MouseUp fires after Click, so reset here to ensure state is cleared
            dropDown.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    // Use SetButtonDrawState() if available, otherwise set State to Normal
                    if (setButtonDrawStateMethod != null)
                    {
                        setButtonDrawStateMethod.Invoke(dropDown, null);
                    }
                    else
                    {
                        ResetDropDownButtonState(dropDown);
                    }
                }
            };
            
            dropDown.MouseLeave += (s, e) =>
            {
                // Use SetButtonDrawState() if available, otherwise set State to Normal
                if (setButtonDrawStateMethod != null)
                {
                    setButtonDrawStateMethod.Invoke(dropDown, null);
                }
                else
                {
                    ResetDropDownButtonState(dropDown);
                }
            };
            
            // Also reset on Click event to ensure state is cleared immediately
            dropDown.Click += (s, e) =>
            {
                // Use SetButtonDrawState() if available, otherwise set State to Normal
                if (setButtonDrawStateMethod != null)
                {
                    setButtonDrawStateMethod.Invoke(dropDown, null);
                }
                else
                {
                    ResetDropDownButtonState(dropDown);
                }
            };
            
            // Add hover effect handlers
            dropDown.MouseEnter += (s, e) => 
            {
                if (dropDown.Enabled)
                {
                    dropDown.Cursor = Cursors.Hand;
                    dropDown.Invalidate();
                }
            };
        }
        
        /// <summary>
        /// Recursively sets up effects for all PoisonButtons and PoisonDropDownButtons in a container
        /// </summary>
        /// <param name="container">The container control to search</param>
        public static void SetupAllButtonEffectsRecursive(Control container)
        {
            if (container == null) return;
            
            foreach (Control ctrl in container.Controls)
            {
                if (ctrl is PoisonButton btn)
                {
                    SetupButtonEffects(btn);
                }
                else if (ctrl is PoisonDropDownButton dropDown)
                {
                    SetupDropDownButtonEffects(dropDown);
                }
                
                if (ctrl.HasChildren)
                {
                    SetupAllButtonEffectsRecursive(ctrl);
                }
            }
        }
    }
}

