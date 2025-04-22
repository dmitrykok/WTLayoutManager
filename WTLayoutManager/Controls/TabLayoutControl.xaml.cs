using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WTLayoutManager.ViewModels;

/// <summary>
/// Represents a custom UserControl for managing tab layouts with dynamic grid-based pane rendering.
/// </summary>
/// <remarks>
/// This control uses a TabStateViewModel to dynamically create a grid layout with configurable rows, columns, 
/// and panes, each with customizable positioning and visual properties.
/// </remarks>
namespace WTLayoutManager.Controls
{
    /// <summary>
    /// Represents a custom UserControl for managing dynamic tab layouts with grid-based pane rendering.
    /// </summary>
    /// <remarks>
    /// This partial class provides a flexible control for creating configurable tab layouts with dynamic grid positioning and visual properties.
    /// </remarks>
    public partial class TabLayoutControl : UserControl
    {
        /// <summary>
        /// Defines the dependency property for the <see cref="TabState"/> property, enabling data binding and change notification.
        /// </summary>
        /// <remarks>
        /// Registers a dependency property with a property metadata that includes a change callback to rebuild the grid layout
        /// when the <see cref="TabStateViewModel"/> is modified.
        /// </remarks>
        public static readonly DependencyProperty TabStateProperty =
            DependencyProperty.Register(nameof(TabState), typeof(TabStateViewModel), typeof(TabLayoutControl),
                new PropertyMetadata(null, OnTabStateChanged));

        /// <summary>
        /// Gets or sets the current tab state view model for the layout control.
        /// </summary>
        /// <value>
        /// A <see cref="TabStateViewModel"/> representing the configuration and state of the tab layout.
        /// </value>
        public TabStateViewModel TabState
        {
            get { return (TabStateViewModel)GetValue(TabStateProperty); }
            set { SetValue(TabStateProperty, value); }
        }

        /// <summary>
        /// Handles the change of the <see cref="TabStateProperty"/> and rebuilds the grid layout.
        /// </summary>
        /// <param name="d">The dependency object whose property changed.</param>
        /// <param name="e">The event arguments of the property change.</param>
        private static void OnTabStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TabLayoutControl control)
            {
                control.BuildGrid();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TabLayoutControl"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor initializes the control and its components, preparing the control for use.
        /// </remarks>
        public TabLayoutControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Rebuilds the visual grid from the <see cref="TabStateViewModel"/> data.
        /// </summary>
        /// <remarks>
        /// This method creates the necessary grid rows and columns, and then adds each <see cref="PaneViewModel"/> as a visual element
        /// into the grid, using the view model's grid position and size information. The visual elements are added as children to the
        /// <see cref="Grid"/> control, with the proper row, column, row span, and column span set.
        /// </remarks>
        private void BuildGrid()
        {
            MainGrid.Children.Clear();
            MainGrid.RowDefinitions.Clear();
            MainGrid.ColumnDefinitions.Clear();

            if (TabState == null)
                return;

            // Create the Grid definitions using the view model's grid size.
            for (int i = 0; i < TabState.GridRows; i++)
            {
                MainGrid.RowDefinitions.Add(new RowDefinition());
            }
            for (int j = 0; j < TabState.GridColumns; j++)
            {
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            // Add each pane into the grid.
            foreach (var pane in TabState.Panes)
            {
                // Create a visual for the pane
                var border = new Border
                {
                    BorderBrush = Brushes.DarkGray,
                    BorderThickness = new Thickness(1.5),
                    Margin = new Thickness(1)
                };

                var stack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };
                var img = new Image
                {
                    Width = 16,
                    Height = 16,
                    Margin = new Thickness(0, 0, 4, 0)
                };

                try
                {
                    if (pane.Icon != null)
                    {
                        img.Source = new BitmapImage(new Uri(pane.Icon, UriKind.RelativeOrAbsolute));
                    }
                }
                catch
                {
                    // Fallback in case of a bad URI
                    img.Source = null;
                }

                var txt = new TextBlock
                {
                    Text = pane.ProfileName,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White,
                    //Background = Brushes.Black,
                    FontSize = 10
                };

                stack.Children.Add(img);
                stack.Children.Add(txt);
                border.Child = stack;

                // Place in the grid using the view model values.
                Grid.SetRow(border, pane.GridRow);
                Grid.SetColumn(border, pane.GridColumn);
                Grid.SetRowSpan(border, pane.GridRowSpan);
                Grid.SetColumnSpan(border, pane.GridColumnSpan);

                MainGrid.Children.Add(border);
            }
        }
    }
}
