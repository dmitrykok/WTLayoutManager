using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WTLayoutManager.ViewModels;

namespace WTLayoutManager.Controls
{
    public partial class TabLayoutControl : UserControl
    {
        public static readonly DependencyProperty TabStateProperty =
            DependencyProperty.Register(nameof(TabState), typeof(TabStateViewModel), typeof(TabLayoutControl),
                new PropertyMetadata(null, OnTabStateChanged));

        public TabStateViewModel TabState
        {
            get { return (TabStateViewModel)GetValue(TabStateProperty); }
            set { SetValue(TabStateProperty, value); }
        }

        private static void OnTabStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TabLayoutControl control)
            {
                control.BuildGrid();
            }
        }

        public TabLayoutControl()
        {
            InitializeComponent();
        }

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
