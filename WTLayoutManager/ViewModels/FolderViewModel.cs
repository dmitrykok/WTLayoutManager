using System.Windows.Input;
using WTLayoutManager.Models;

namespace WTLayoutManager.ViewModels
{
    public class FolderViewModel : BaseViewModel
    {
        private readonly FolderModel _folder;

        public FolderViewModel(FolderModel folder)
        {
            _folder = folder;
            // Initialize commands
            RunCommand = new RelayCommand(ExecuteRun);
            RunAsCommand = new RelayCommand(ExecuteRunAs);
            DuplicateCommand = new RelayCommand(ExecuteDuplicate);
            DeleteCommand = new RelayCommand(ExecuteDelete);
            OpenFolderCommand = new RelayCommand(ExecuteOpenFolder);
        }

        public string Name
        {
            get => _folder.Name;
            set
            {
                if (_folder.Name != value)
                {
                    _folder.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDefault => _folder.IsDefault;

        public DateTime? LastRun
        {
            get => _folder.LastRun;
            set
            {
                if (_folder.LastRun != value)
                {
                    _folder.LastRun = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<FileModel> Files => _folder.Files; // or new ObservableCollection if needed

        // Expand/Collapse handling in the main grid can be done at the MainViewModel level, or here with a boolean.

        // Commands
        public ICommand RunCommand { get; }
        public ICommand RunAsCommand { get; }
        public ICommand DuplicateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand OpenFolderCommand { get; }

        private void ExecuteRun(object? parameter)
        {
            // Logic to run terminal with current user privileges
        }

        private void ExecuteRunAs(object? parameter)
        {
            // Logic to run terminal as admin (UAC prompt)
        }

        private void ExecuteDuplicate(object? parameter)
        {
            // Logic to duplicate folder on disk, 
            // then notify the main list that a new folder is available
        }

        private void ExecuteDelete(object? parameter)
        {
            // Logic to delete from disk, 
            // show error if locked, etc.
        }

        private void ExecuteOpenFolder(object? parameter)
        {
            // Logic to open folder in Explorer
        }
    }
}
