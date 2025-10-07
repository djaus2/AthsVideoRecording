using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendVideoOverTCPLib.Converters
{
    public class IpSelectedToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ip = value as string;
            bool isVisible = string.IsNullOrEmpty(ip); // Picker visible when IP is empty

            if (parameter?.ToString() == "Invert")
                isVisible = !isVisible;

            return isVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
