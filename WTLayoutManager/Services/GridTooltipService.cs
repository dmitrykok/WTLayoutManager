using System.Windows;

/// <summary>
/// Provides an attached dependency property and methods for managing tooltip profiles on framework elements.
/// </summary>
/// <remarks>
/// This service allows setting and getting tooltip profiles for dependency objects,
/// automatically updating the ToolTip property when the profiles change.
/// </remarks>
namespace WTLayoutManager.Services
{
    public static class GridTooltipService
    {
        public static readonly DependencyProperty TooltipProfilesProperty =
            DependencyProperty.RegisterAttached(
                "TooltipProfiles",
                typeof(object),
                typeof(GridTooltipService),
                new PropertyMetadata(null, OnTooltipProfilesChanged));

        /// <summary>
        /// Sets the tooltip profiles for a dependency object.
        /// </summary>
        /// <param name="element">The dependency object.</param>
        /// <param name="value">The tooltip profiles.</param>
        public static void SetTooltipProfiles(DependencyObject element, object value)
        {
            element.SetValue(TooltipProfilesProperty, value);
        }

        /// <summary>
        /// Gets the tooltip profiles for a dependency object.
        /// </summary>
        /// <param name="element">The dependency object.</param>
        /// <returns>The tooltip profiles.</returns>
        public static object GetTooltipProfiles(DependencyObject element)
        {
            return element.GetValue(TooltipProfilesProperty);
        }

        /// <summary>
        /// Handles the change of the <see cref="TooltipProfilesProperty"/> attached property.
        /// </summary>
        /// <param name="d">The dependency object whose property changed.</param>
        /// <param name="e">The event arguments of the property change.</param>
        private static void OnTooltipProfilesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                if (e.NewValue != null)
                {
                    element.ToolTip = e.NewValue;
                }
                else
                {
                    element.ToolTip = null;
                }
            }
        }
    }
}
