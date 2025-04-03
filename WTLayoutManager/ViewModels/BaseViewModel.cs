using System.ComponentModel;
using System.Runtime.CompilerServices;
using WTLayoutManager.Services;

namespace WTLayoutManager.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected readonly IMessageBoxService _messageBoxService;

        public BaseViewModel(IMessageBoxService messageBoxService)
        {
            _messageBoxService = messageBoxService;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
