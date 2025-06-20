using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using System.Windows.Forms;  // Add this line
using System.Collections;

namespace Refract.UI.Core.Controls
{
    internal class CBFInnerPanelDesigner : ParentControlDesigner
    {
        public override SelectionRules SelectionRules
        {
            get
            {
                var rules = base.SelectionRules;
                rules &= ~SelectionRules.AllSizeable;
                return rules;
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

            string[] propsToRemove = {
                "Dock", "Anchor",
                "Size", "Location", "Width", "Height",
                "MinimumSize", "MaximumSize",
                "AutoSize", "AutoSizeMode",
                "Visible", "Enabled"
            };

            foreach (var prop in propsToRemove)
            {
                if (properties.Contains(prop))
                {
                    properties[prop] = TypeDescriptor.CreateProperty(
                        Component.GetType(),
                        (PropertyDescriptor)properties[prop],
                        new BrowsableAttribute(false));
                }
            }
        }
    }
}