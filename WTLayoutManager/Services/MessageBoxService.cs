using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WTLayoutManager.Views;

namespace WTLayoutManager.Services
{
    public class MessageBoxService : IMessageBoxService
    {
        private Window? GetOwnerWindow() => Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);

        public bool Confirm(string message, string title = "Confirm")
        {
            var dialog = new ConfirmationDialog(message, title)
            {
                Owner = GetOwnerWindow()
            };
            return dialog.ShowDialog() == true;
        }

        public void ShowMessage(string message, string title = "Information", DialogType dialogType = DialogType.Information)
        {
            var dialog = new CustomMessageBox(message, title, dialogType)
            {
                Owner = GetOwnerWindow()
            };
            dialog.ShowDialog();
        }
    }

}
