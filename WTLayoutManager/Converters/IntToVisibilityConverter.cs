using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
/// A value converter that transforms an integer to a <see cref="Visibility"/> value.
/// </summary>
/// <remarks>
/// Converts integers to <see cref="Visibility.Visible"/> when the value is greater than zero,
/// and <see cref="Visibility.Collapsed"/> otherwise. Implements <see cref="IValueConverter"/> for use in data binding scenarios.
/// </remarks>
namespace WTLayoutManager.Converters
{
    public class IntToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value to a <see cref="Visibility"/> value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        ///     <see cref="Visibility.Visible"/> if the value is greater than 0; otherwise <see cref="Visibility.Collapsed"/>.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue > 0 ? Visibility.Visible : Visibility.Collapsed;

            return Visibility.Collapsed;
        }

        /// <summary>
        /// This method is not supported. It is required by the <see cref="IValueConverter"/> interface.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        ///     Always throws <see cref="NotSupportedException"/>.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

}
