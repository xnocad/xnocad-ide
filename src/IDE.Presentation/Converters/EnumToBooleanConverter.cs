﻿namespace IDE.Core.Converters
{
	using System.Windows.Data;
	using System.Globalization;
	using System;

	/// <summary>
	/// This class can be used to databind a (group of) radio button control(s)
	/// with an enumeration in a ViewModel.
	/// 
	/// Source: http://www.wpftutorial.net/RadioButton.html
	/// </summary>
	public class EnumToBooleanConverter : IValueConverter
	{
		/// <summary>
		/// Enum to Boolean Converter method
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null || parameter == null)
				return false;

			var checkValue = value.ToString();
			var targetValue = parameter.ToString();

			var bRet = checkValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);

			return bRet;
		}

		/// <summary>
		/// Boolean to Enum Converter method
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null || parameter == null)
				return null;

			var useValue = (bool)value;
			var targetValue = parameter.ToString();
			if (useValue)
				return Enum.Parse(targetType, targetValue);

			return null;
		}
	}
}
