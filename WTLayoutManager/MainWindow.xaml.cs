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
    /// Represents the main window of the WTLayoutManager application.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes the main window of the WTLayoutManager application.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the click event for confirming an edit in a DataGrid cell.
        /// </summary>
        /// <param name="sender">The source of the event, expected to be a Button.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method finds the DataGridCell and TextBox associated with the Button click,
        /// updates the source binding of the TextBox, and commits the edit to the DataGrid.
        /// </remarks>
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

        /// <summary>
        /// Handles the click event for canceling an edit in a DataGrid cell.
        /// </summary>
        /// <param name="sender">The source of the event, expected to be a Button.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method finds the DataGrid associated with the Button click and
        /// cancels the edit in the cell.
        /// </remarks>
        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            var dataGrid = FindAncestor<DataGrid>((DependencyObject)sender);
            dataGrid?.CancelEdit(DataGridEditingUnit.Cell);
        }

        /// <summary>
        /// Handles the loaded event for a TextBox used for DataGridCell editing.
        /// </summary>
        /// <param name="sender">The source of the event, expected to be a TextBox.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method is used to ensure that the TextBox is focused, receives keyboard input,
        /// and selects all its text when the editing begins.
        /// Additionally, it handles Enter and Escape key events, committing and canceling the edit respectively.
        /// </remarks>
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

        /// <summary>
        /// Finds an ancestor of a given type in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of ancestor to find.</typeparam>
        /// <param name="current">The current element in the visual tree.</param>
        /// <returns>The ancestor of the given type, or the default value if none is found.</returns>
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

        /// <summary>
        /// Finds a child of a given type in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of child to find.</typeparam>
        /// <param name="parent">The parent element in the visual tree.</param>
        /// <returns>The child of the given type, or the default value if none is found.</returns>
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