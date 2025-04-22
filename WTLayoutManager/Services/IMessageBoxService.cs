using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WTLayoutManager.Services
{
    /// <summary>
    /// Represents the type of dialog to be displayed in message box interactions.
    /// </summary>
    /// <remarks>
    /// Defines the different styles of dialogs that can be used when showing messages to the user.
    /// </remarks>
    public enum DialogType
    {
        Information,
        Warning,
        Error,
        Confirmation
    }

    /// <summary>
    /// Provides methods for displaying message boxes and handling user confirmations.
    /// </summary>
    /// <remarks>
    /// This service abstracts message box interactions, allowing for consistent dialog presentation across the application.
    /// </remarks>
    public interface IMessageBoxService
    {
        bool Confirm(string message, string title = "Confirm");
        void ShowMessage(string message, string title = "Information", DialogType dialogType = DialogType.Information);
    }
}