using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
                    Icon.Kind = PackIconKind.InformationOutline;
                    Icon.Foreground = Brushes.DodgerBlue;
                    break;
                case DialogType.Warning:
                    Icon.Kind = PackIconKind.AlertOutline;
                    Icon.Foreground = Brushes.Orange;
                    break;
                case DialogType.Error:
                    Icon.Kind = PackIconKind.AlertCircleOutline;
                    Icon.Foreground = Brushes.Red;
                    break;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }

}
