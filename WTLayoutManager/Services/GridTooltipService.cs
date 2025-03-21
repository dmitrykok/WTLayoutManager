using System.Windows;

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

        public static void SetTooltipProfiles(DependencyObject element, object value)
        {
            element.SetValue(TooltipProfilesProperty, value);
        }

        public static object GetTooltipProfiles(DependencyObject element)
        {
            return element.GetValue(TooltipProfilesProperty);
        }

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
