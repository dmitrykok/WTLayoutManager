using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WTLayoutManager.Converters
{
    public class StateJsonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value as string)?.Equals("state.json", StringComparison.OrdinalIgnoreCase) == true || 
            (value as string)?.Equals("elevated-state.json", StringComparison.OrdinalIgnoreCase) == true
                ? Visibility.Visible
                : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
