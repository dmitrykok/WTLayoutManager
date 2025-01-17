using System.Windows.Input;
using WTLayoutManager.Models;

namespace WTLayoutManager.ViewModels
{
    public class FolderViewModel : BaseViewModel
    {
        private string _isExpanded = "Collapsed";
        private readonly FolderModel _folder;
        private readonly MainViewModel _parentViewModel;

        public FolderViewModel(FolderModel folder, MainViewModel parentViewModel)
        {
            _folder = folder;
            _parentViewModel = parentViewModel;
            // Initialize commands
            RunCommand = new RelayCommand(ExecuteRun);
            RunAsCommand = new RelayCommand(ExecuteRunAs);
            DuplicateCommand = new RelayCommand(ExecuteDuplicate);
            DeleteCommand = new RelayCommand(ExecuteDelete);
            OpenFolderCommand = new RelayCommand(ExecuteOpenFolder);
            // ToggleExpandCollapseCommand = new RelayCommand(_ => OnToggleExpandCollapse());
        }

        public string IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ExpandCollapseIndicator));
                }
            }
        }

        public string ExpandCollapseIndicator
        {
            get => IsExpanded == "Collapsed" ? "+" : "-";
        }

        // We'll also add a command to toggle IsExpanded:
        private ICommand _toggleExpandCollapseCommand;
        public ICommand ToggleExpandCollapseCommand
        {
            get
            {
                return _toggleExpandCollapseCommand ??= new RelayCommand(_ =>
                {
                    // Let's assume we have a reference to MainViewModel or a callback
                    // to collapse other rows. For instance:
                    _parentViewModel.CollapseAllExcept(this);

                    if (IsExpanded == "Collapsed")
                    {
                        IsExpanded = "Visible";
                    }
                    else
                    {
                        IsExpanded = "Collapsed";
                    }
                });
            }
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

        public string Path
        {
            get => _folder.Path;
            set
            {
                if (_folder.Path != value)
                {
                    _folder.Path = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDefault => _folder.IsDefault;
        public bool CanDelete => !_folder.IsDefault;

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
