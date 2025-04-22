using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
/// A value converter that determines UI visibility based on whether the input value is "settings.json".
/// </summary>
/// <remarks>
/// Implements <see cref="IValueConverter"/> to provide a conversion from a string value to <see cref="Visibility"/>.
/// Returns <see cref="Visibility.Visible"/> if the input is "settings.json" (case-insensitive), otherwise returns <see cref="Visibility.Collapsed"/>.
/// </remarks>
namespace WTLayoutManager.Converters
{
    public class SettingsJsonVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value to a <see cref="Visibility"/> value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        ///     <see cref="Visibility.Visible"/> if the value is equal to "settings.json" (case-insensitive);
        ///     otherwise <see cref="Visibility.Collapsed"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value as string)?.Equals("settings.json", StringComparison.OrdinalIgnoreCase) == true
                ? Visibility.Visible
                : Visibility.Collapsed;

        /// <summary>
        /// This method is not supported. It is required by the <see cref="IValueConverter"/> interface.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        ///     Always throws <see cref="NotImplementedException"/>.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
