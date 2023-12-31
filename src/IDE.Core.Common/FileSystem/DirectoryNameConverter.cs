﻿namespace IDE.Core
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    public class DirectoryNameConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(DirectoryName) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                return DirectoryName.Create((string)value);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture,
                                                                         object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return value.ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
