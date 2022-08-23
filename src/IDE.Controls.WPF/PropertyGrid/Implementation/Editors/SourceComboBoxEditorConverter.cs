﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace IDE.Controls.WPF.PropertyGrid.Editors;

internal class SourceComboBoxEditorConverter : IValueConverter
{
    private TypeConverter _typeConverter;

    internal SourceComboBoxEditorConverter(TypeConverter typeConverter)
    {
        _typeConverter = typeConverter;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (_typeConverter != null)
        {
            if (_typeConverter.CanConvertTo(typeof(string)))
                return _typeConverter.ConvertTo(value, typeof(string));
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (_typeConverter != null)
        {
            if (_typeConverter.CanConvertFrom(value.GetType()))
                return _typeConverter.ConvertFrom(value);
        }
        return value;
    }
}