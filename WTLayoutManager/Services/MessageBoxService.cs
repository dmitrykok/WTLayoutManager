using System.Windows;
using WTLayoutManager.Views;

/// <summary>
/// Provides services for displaying message boxes and confirmation dialogs in the application.
/// </summary>
/// <remarks>
/// Implements <see cref="IMessageBoxService"/> to handle user interactions through custom dialog windows.
/// </remarks>
namespace WTLayoutManager.Services
{
    public class MessageBoxService : IMessageBoxService
    {
        /// <summary>
        /// Retrieves the currently active window from the application's window collection.
        /// </summary>
        /// <returns>The active window, or null if no window is currently active.</returns>
        private Window? GetOwnerWindow() => Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);

        /// <summary>
        /// Shows a confirmation dialog box with the specified message and title.
        /// </summary>
        /// <param name="message">The message to be displayed in the confirmation dialog box.</param>
        /// <param name="title">The title of the confirmation dialog box. Defaults to "Confirm".</param>
        /// <returns>true if the user confirms, otherwise false.</returns>
        public bool Confirm(string message, string title = "Confirm")
        {
            var dialog = new ConfirmationDialog(message, title)
            {
                Owner = GetOwnerWindow()
            };
            return dialog.ShowDialog() == true;
        }

        /// <summary>
        /// Shows a message box with the specified message and title.
        /// </summary>
        /// <param name="message">The message to be displayed in the message box.</param>
        /// <param name="title">The title of the message box. Defaults to "Information".</param>
        /// <param name="dialogType">The type of message box to display. Defaults to <see cref="DialogType.Information"/>.</param>
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
