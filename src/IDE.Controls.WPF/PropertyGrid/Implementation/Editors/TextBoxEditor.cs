﻿using System.ComponentModel.DataAnnotations;
using System.Windows.Controls;
using IDE.Controls.WPF.Core.Extensions;

namespace IDE.Controls.WPF.PropertyGrid.Editors;

public class TextBoxEditor : TypeEditor<AdvancedTextBox>
{
    protected override AdvancedTextBox CreateEditor()
    {
        return new PropertyGridEditorTextBox();
    }

    protected override void SetControlProperties(PropertyItem propertyItem)
    {
        var displayAttribute = propertyItem.PropertyDescriptor.GetAttribute<DisplayAttribute>();
        if (displayAttribute != null)
        {
            Editor.Watermark = displayAttribute.GetPrompt();
        }
    }

    protected override void SetValueDependencyProperty()
    {
        ValueProperty = TextBox.TextProperty;
    }
}
