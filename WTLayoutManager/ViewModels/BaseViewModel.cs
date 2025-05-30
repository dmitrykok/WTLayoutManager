using System.ComponentModel;
using System.Runtime.CompilerServices;
using WTLayoutManager.Services;

namespace WTLayoutManager.ViewModels
{
    /// <summary>
    /// Base view model that implements the INotifyPropertyChanged interface for property change notifications.
    /// </summary>
    /// <remarks>
    /// Provides a common base implementation for view models in the WTLayoutManager application,
    /// enabling automatic property change notifications and centralized message box service access.
    /// </remarks>
    public class BaseViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        /// <remarks>
        /// This event is part of the INotifyPropertyChanged interface implementation,
        /// allowing clients to be notified of property changes in the view model.
        /// </remarks>
        public event PropertyChangedEventHandler? PropertyChanged;
        /// <summary>
        /// Gets the message box service used for displaying dialogs and messages.
        /// </summary>
        /// <remarks>
        /// This protected readonly field allows derived view models to access the message box service
        /// for showing user notifications, alerts, and dialog boxes.
        /// </remarks>
        protected readonly IMessageBoxService _messageBoxService;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseViewModel"/> class.
        /// </summary>
        /// <param name="messageBoxService">The message box service used for displaying dialogs and messages.</param>
        public BaseViewModel(IMessageBoxService messageBoxService)
        {
            _messageBoxService = messageBoxService;
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the given
        /// <paramref name="propertyName"/>, if it is not null or empty.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
