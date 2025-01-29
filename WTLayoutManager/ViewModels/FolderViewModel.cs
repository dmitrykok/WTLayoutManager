using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Windows.Input;
using WTLayoutManager.Models;

namespace WTLayoutManager.ViewModels
{
    public class FolderViewModel : BaseViewModel
    {
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

        private bool _isExpanded = false;
        public bool IsExpanded
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

        public string ExpandCollapseIndicator => IsExpanded ? "-" : "+";

        // We'll also add a command to toggle IsExpanded:
        private ICommand _toggleExpandCollapseCommand;
        public ICommand ToggleExpandCollapseCommand
        {
            get
            {
                return _toggleExpandCollapseCommand ??= new RelayCommand(_ =>
                {
                    // Collapse all others via the parent
                    _parentViewModel.CollapseAllExcept(this);
                    // Toggle myself
                    IsExpanded = !IsExpanded;
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
            try
            {
                // 1) Get the TerminalInfo from the parent (or the parent's dictionary)
                //    Typically, you'd do something like:
                //    var terminalInfo = _parentViewModel.GetCurrentTerminalInfo();
                //    But since we have multiple terminals, let's assume we
                //    can retrieve the relevant one from the parent's _terminalDict
                //    keyed by the parent's SelectedTerminal.

                // As an example:
                if (_parentViewModel.TerminalDict != null &&
                    _parentViewModel.SelectedTerminal != null)
                {
                    var key = _parentViewModel.SelectedTerminal.DisplayName;
                    if (_parentViewModel.TerminalDict.TryGetValue(key, out var terminalInfo))
                    {
                        // 2) Compose the command line to run Windows Terminal
                        //    For instance, if it's registered, we can just "wt.exe" plus arguments.
                        //    If you want a custom config path, you might do:
                        //       wt.exe --settings {Path}\settings.json
                        //    Or pass the folder as a starting directory, etc.
                        var fileName = System.IO.Path.Combine(terminalInfo.InstalledLocationPath, "wt.exe");
                        if ( !File.Exists(fileName) )
                        {
                            fileName = System.IO.Path.Combine(terminalInfo.InstalledLocationPath, "wtd.exe");
                        }

                        var psi = new ProcessStartInfo
                        {
                            FileName = fileName,
                            WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            //Arguments = $"--title \"{Name}\" --settings \"{Path}\\settings.json\"",
                            UseShellExecute = true           // must be true for normal user run
                        };

                        Process.Start(psi);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to run terminal.\n{ex.Message}",
                                "WTLayoutManager",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void ExecuteRunAs(object? parameter)
        {
            try
            {
                // Similar approach, but specify Verb="runas" to request admin privileges
                if (_parentViewModel.TerminalDict != null &&
                    _parentViewModel.SelectedTerminal != null)
                {
                    var key = _parentViewModel.SelectedTerminal.DisplayName;
                    if (_parentViewModel.TerminalDict.TryGetValue(key, out var terminalInfo))
                    {
                        var fileName = System.IO.Path.Combine(terminalInfo.InstalledLocationPath, "wt.exe");
                        if (!File.Exists(fileName))
                        {
                            fileName = System.IO.Path.Combine(terminalInfo.InstalledLocationPath, "wtd.exe");
                        }

                        var psi = new ProcessStartInfo
                        {
                            FileName = fileName,
                            WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            //Arguments = $"--title \"{Name}\" --settings \"{Path}\\settings.json\"",
                            UseShellExecute = true,
                            Verb = "runas" // triggers UAC prompt
                        };

                        Process.Start(psi);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to run terminal as admin.\n{ex.Message}",
                                "WTLayoutManager",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }


        private void ExecuteDuplicate(object? parameter)
        {
            try
            {
                if (_parentViewModel.TerminalDict != null &&
                    _parentViewModel.SelectedTerminal != null)
                {
                    var key = _parentViewModel.SelectedTerminal.DisplayName;
                    if (_parentViewModel.TerminalDict.TryGetValue(key, out var terminalInfo))
                    {
                        // 1) Build a new folder name, e.g. "LocalState_20230131_123456"
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string newFolderName = $"LocalState_{timestamp}";

                        // 2) The parent folder is e.g. %LOCALAPPDATA%\WTLayoutManager\<familyName>
                        //    But let's just assume we replicate the same directory structure as "Path"
                        //    Or we rely on the parent's logic if needed.

                        // Let's say we want to copy into the same parent directory:
                        string customBasePath = System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "WTLayoutManager",
                            terminalInfo.FamilyName);

                        string destinationPath = System.IO.Path.Combine(customBasePath, newFolderName);

                        // 3) Copy everything
                        DirectoryCopy(Path, destinationPath);

                        // 4) Create a new FolderModel / FolderViewModel for the copy
                        var newFolderModel = new FolderModel
                        {
                            Name = newFolderName,
                            Path = destinationPath,
                            IsDefault = false,
                            Files = new List<FileModel>()
                        };
                        // Optionally, copy over .LastRun or anything else

                        // Populate its .Files if needed
                        // The parent's Load logic might do it automatically,
                        // but we can do a quick load of the 3 special json files if we want:

                        var interestingFiles = new[] { "state.json", "elevated-state.json", "settings.json" };
                        foreach (string fileName in interestingFiles)
                        {
                            string fullPath = System.IO.Path.Combine(destinationPath, fileName);
                            if (File.Exists(fullPath))
                            {
                                var fi = new FileInfo(fullPath);
                                newFolderModel.Files.Add(new FileModel
                                {
                                    FileName = fi.Name,
                                    LastModified = fi.LastWriteTime,
                                    Size = fi.Length
                                });
                            }
                        }

                        // 5) Add it to the parent's Folders collection => new row in DataGrid
                        var newFolderViewModel = new FolderViewModel(newFolderModel, _parentViewModel);
                        _parentViewModel.Folders.Add(newFolderViewModel);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to duplicate folder.\n{ex.Message}",
                                "WTLayoutManager",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        // Helper method: copy directory recursively
        private void DirectoryCopy(string sourceDir, string destDir)
        {
            var dirInfo = new DirectoryInfo(sourceDir);
            if (!dirInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
            }

            Directory.CreateDirectory(destDir);

            // Copy files
            foreach (FileInfo file in dirInfo.GetFiles())
            {
                string targetFilePath = System.IO.Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            // Recursively copy subdirectories
            foreach (DirectoryInfo subDir in dirInfo.GetDirectories())
            {
                string newDest = System.IO.Path.Combine(destDir, subDir.Name);
                DirectoryCopy(subDir.FullName, newDest);
            }
        }


        private void ExecuteDelete(object? parameter)
        {
            try
            {
                if (IsDefault)
                {
                    MessageBox.Show("Cannot delete the default LocalState folder.",
                                    "WTLayoutManager",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }

                // Attempt to delete from disk
                Directory.Delete(Path, true); // true => recursive
                                              // If it's locked, an IOException or UnauthorizedAccessException might be thrown

                // Remove it from the parent's Folders collection
                _parentViewModel.Folders.Remove(this);
            }
            catch (Exception ex)
            {
                // Possibly the folder is locked (Terminal is running?), or permission error
                MessageBox.Show($"Failed to delete folder.\n{ex.Message}",
                                "WTLayoutManager",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }


        private void ExecuteOpenFolder(object? parameter)
        {
            try
            {
                // Just call explorer.exe on the folder path
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{Path}\"",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open folder.\n{ex.Message}",
                                "WTLayoutManager",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

    }
}
