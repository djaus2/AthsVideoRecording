using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace AthsVideoRecording.Converters
{
    public class EnumEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            // Try to parse the parameter as the same enum type as the value
            if (value.GetType().IsEnum && parameter is string paramString)
            {
                try
                {
                    // Parse the string parameter to the enum type
                    var enumValue = Enum.Parse(value.GetType(), paramString);
                    return value.Equals(enumValue);
                }
                catch
                {
                    // If parsing fails, return false
                    return false;
                }
            }

            // Direct comparison if both are of the same type
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
