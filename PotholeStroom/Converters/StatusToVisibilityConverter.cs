using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace PotholeStroom.Converters
{
    public class StatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;
            if (string.IsNullOrWhiteSpace(status))
                return true; // default to visible

            // Hide donate section if status is "In Progress" or "Finished"
            return !(status.Equals("In Progress", StringComparison.OrdinalIgnoreCase) ||
                     status.Equals("Finished", StringComparison.OrdinalIgnoreCase));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
