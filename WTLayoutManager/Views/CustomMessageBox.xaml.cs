using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Media;
using WTLayoutManager.Services;

namespace WTLayoutManager.Views
{
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox(string message, string title, DialogType dialogType)
        {
            InitializeComponent();

            MessageText.Text = message;
            MessageText.TextWrapping = TextWrapping.Wrap;
            TitleText.Text = title;
            Title = title;

            switch (dialogType)
            {
                case DialogType.Information:
                    MessageIcon.Kind = PackIconKind.InformationOutline;
                    MessageIcon.Foreground = Brushes.DodgerBlue;
                    break;
                case DialogType.Warning:
                    MessageIcon.Kind = PackIconKind.AlertOutline;
                    MessageIcon.Foreground = Brushes.Orange;
                    break;
                case DialogType.Error:
                    MessageIcon.Kind = PackIconKind.AlertCircleOutline;
                    MessageIcon.Foreground = Brushes.Red;
                    break;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }

}
