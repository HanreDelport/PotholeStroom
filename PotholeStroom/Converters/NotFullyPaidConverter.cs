using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace PotholeStroom.Converters
{
    public class NotFullyPaidConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double remaining && remaining > 0)
                return true; // Show costs if not fully paid
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
