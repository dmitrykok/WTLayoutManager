using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WTLayoutManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ConfirmEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var cell = FindAncestor<DataGridCell>(btn);
                if (cell != null)
                {
                    var textBox = FindChild<TextBox>(cell);
                    textBox?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

                    var dataGrid = FindAncestor<DataGrid>(cell);
                    dataGrid?.CommitEdit(DataGridEditingUnit.Cell, true);
                }
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            var dataGrid = FindAncestor<DataGrid>((DependencyObject)sender);
            dataGrid?.CancelEdit(DataGridEditingUnit.Cell);
        }

        private void EditTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.Dispatcher.InvokeAsync(() =>
                {
                    textBox.Focus();
                    Keyboard.Focus(textBox);
                    textBox.SelectAll();
                }, DispatcherPriority.Input);

                // Optional: Handle Enter/Escape key events directly here
                textBox.KeyDown += (s, args) =>
                {
                    if (args.Key == Key.Enter)
                    {
                        DataGrid? dataGrid = FindAncestor<DataGrid>(textBox);
                        dataGrid?.CommitEdit(DataGridEditingUnit.Cell, true);
                    }
                    else if (args.Key == Key.Escape)
                    {
                        DataGrid? dataGrid = FindAncestor<DataGrid>(textBox);
                        dataGrid?.CancelEdit(DataGridEditingUnit.Cell);
                    }
                };
            }
        }

        // Helper method to find ancestor in visual tree
        private T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T target)
                    return target;

                current = VisualTreeHelper.GetParent(current);
            }
            return default;
        }

        private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var descendant = FindChild<T>(child);
                if (descendant != null)
                    return descendant;
            }

            return null;
        }

    }
}