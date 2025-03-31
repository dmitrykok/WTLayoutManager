using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using WTLayoutManager.Models;
using WTLayoutManager.Services;

namespace WTLayoutManager.ViewModels
{
    public class FolderViewModel : BaseViewModel
    {
        private readonly FolderModel _folder;
        private readonly MainViewModel _parentViewModel;
        private readonly string _localAppRoot;
        private readonly string _localAppBin;
        private readonly IMessageBoxService _messageBoxService;
        private readonly Dictionary<string, Task<int>> _runningTerminals = new Dictionary<string, Task<int>>();
        private readonly Dictionary<string, Task<int>> _runningTerminalsAs = new Dictionary<string, Task<int>>();

        public FolderViewModel(FolderModel folder, MainViewModel parentViewModel, IMessageBoxService messageBoxService)
        {
            _folder = folder;
            _parentViewModel = parentViewModel;
            _messageBoxService = messageBoxService;
            _localAppRoot = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WTLayoutManager");
            _localAppBin = System.IO.Path.Combine(_localAppRoot, "bin");
            // Initialize commands
            RunCommand = new RelayCommand(async _ => await ExecuteRunAsync());
            RunAsCommand = new RelayCommand(async _ => await ExecuteRunAsAsync());
            DuplicateCommand = new RelayCommand(ExecuteDuplicate);
            DeleteCommand = new RelayCommand(ExecuteDelete);
            OpenFolderCommand = new RelayCommand(ExecuteOpenFolder);
            EditFolderCommand = new RelayCommand(ExecuteEditFolderCommand);
            ConfirmEditCommand = new RelayCommand(ExecuteConfirmEditCommand);
            CancelEditCommand = new RelayCommand(ExecuteCancelEditCommand);
        }

        public bool CanRunTerminal => !_runningTerminals.ContainsKey(Path);
        public bool CanRunTerminalAs => !_runningTerminalsAs.ContainsKey(Path);

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
                    //_folder.Name = value;
                    ExecuteDuplicateEx(null, value);
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
                    OnPropertyChanged(nameof(CanRunTerminal));
                    OnPropertyChanged(nameof(CanRunTerminalAs));
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
        public ICommand EditFolderCommand { get; }
        public ICommand ConfirmEditCommand { get; }
        public ICommand CancelEditCommand { get; }

        private async Task ExecuteTerminalAsync(
            Dictionary<string, Task<int>> runningTerminals,
            string alreadyRunningMessage,
            string propertyName,
            Func<string, Task<int>> launchProcess
        )
        {
            if (runningTerminals.ContainsKey(Path))
            {
                _messageBoxService.ShowMessage(alreadyRunningMessage, "Warning", DialogType.Warning);
                return;
            }

            var key = _parentViewModel.SelectedTerminal.DisplayName;
            if (_parentViewModel.TerminalDict?.TryGetValue(key, out var terminalInfo) == true)
            {
                var fileName = System.IO.Path.Combine(terminalInfo.InstalledLocationPath, "WindowsTerminal.exe");
                if (!File.Exists(fileName))
                {
                    fileName = System.IO.Path.Combine(terminalInfo.InstalledLocationPath, "wtd.exe");
                }

                Task<int> launchTask = launchProcess(fileName);
                runningTerminals.Add(Path, launchTask);
                OnPropertyChanged(propertyName);

                int exitCode = await launchTask;
                runningTerminals.Remove(Path);
                OnPropertyChanged(propertyName);

                if (exitCode != 0)
                {
                    _messageBoxService.ShowMessage($"Process exited with code.\n{exitCode}", "Warning", DialogType.Warning);
                }
            }
        }

        private async Task ExecuteRunAsync()
        {
            string envBlock = string.Empty;
            if (!IsDefault)
            {
                envBlock = $"WT_BASE_SETTINGS_PATH={Path}\0"; // single-null terminator per variable
                envBlock += "\0"; // explicit double-null termination for the block
            }

            try
            {
                await ExecuteTerminalAsync(
                    _runningTerminals,
                    "Terminal is already running for this local state.",
                    nameof(CanRunTerminal),
                    fileName => Task.Run(() => ProcessLauncher.LaunchProcess(
                        fileName,
                        $"\"{fileName}\"",
                        envBlock)
                    )
                );
            }
            catch (Exception ex)
            {
                _runningTerminals.Remove(Path);
                OnPropertyChanged(nameof(CanRunTerminal));
                _messageBoxService.ShowMessage($"Failed to run terminal.\n{ex.Message}", "Error", DialogType.Error);
            }
        }

        private async Task ExecuteRunAsAsync()
        {
            string envBlock = string.Empty;
            if (!IsDefault)
            {
                envBlock = $"WT_BASE_SETTINGS_PATH={Path}\0"; // single-null terminator per variable
                envBlock += "\0"; // explicit double-null termination for the block
            }

            try
            {
                await ExecuteTerminalAsync(
                    _runningTerminalsAs,
                    "Terminal Admin is already running for this local state.",
                    nameof(CanRunTerminalAs),
                    fileName => Task.Run(() => ProcessLauncher.LaunchProcessElevated(
                        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ElevatedLauncher.exe"),
                        fileName,
                        $"\"{fileName}\"",
                        envBlock)
                    )
                );
            }
            catch (Exception ex)
            {
                _runningTerminalsAs.Remove(Path);
                OnPropertyChanged(nameof(CanRunTerminalAs));
                _messageBoxService.ShowMessage($"Failed to run terminal.\n{ex.Message}", "Error", DialogType.Error);
            }
        }

        private void ExecuteDuplicate(object? parameter) { ExecuteDuplicateEx(parameter, null); }

        private void ExecuteDuplicateEx(object? parameter, string? exFolderName)
        {
            try
            {
                if (_parentViewModel.TerminalDict != null &&
                    _parentViewModel.SelectedTerminal != null)
                {
                    var key = _parentViewModel.SelectedTerminal.DisplayName;
                    if (_parentViewModel.TerminalDict.TryGetValue(key, out var terminalInfo))
                    {
                        string? newFolderName = exFolderName;
                        if (newFolderName == null)
                        {
                            // 1) Build a new folder name, e.g. "LocalState_20230131_123456"
                            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            newFolderName = $"LocalState_{timestamp}";
                        }

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
                                if (newFolderModel.LastRun == null && fileName == "state.json")
                                {
                                    newFolderModel.LastRun = fi.LastWriteTime;
                                }
                            }
                        }

                        // 5) Add it to the parent's Folders collection => new row in DataGrid
                        var newFolderViewModel = new FolderViewModel(newFolderModel, _parentViewModel, _messageBoxService);
                        _parentViewModel.Folders.Add(newFolderViewModel);
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Failed to duplicate folder.\n{ex.Message}",
                //                "WTLayoutManager",
                //                MessageBoxButton.OK,
                //                MessageBoxImage.Error);
                _messageBoxService.ShowMessage($"Failed to duplicate folder.\n{ex.Message}", "Error", DialogType.Error);
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
                    //MessageBox.Show("Cannot delete the default LocalState folder.",
                    //                "WTLayoutManager",
                    //                MessageBoxButton.OK,
                    //                MessageBoxImage.Warning);
                    _messageBoxService.ShowMessage("Cannot delete the default LocalState folder.", "Warning", DialogType.Warning);
                    return;
                }

                if (_messageBoxService.Confirm($"Are you sure you want to delete '{Name}'?"))
                {
                    // Attempt to delete from disk
                    Directory.Delete(Path, true); // true => recursive
                                                  // If it's locked, an IOException or UnauthorizedAccessException might be thrown

                    // Remove it from the parent's Folders collection
                    _parentViewModel.Folders.Remove(this);
                }
            }
            catch (Exception ex)
            {
                // Possibly the folder is locked (Terminal is running?), or permission error
                //MessageBox.Show($"Failed to delete folder.\n{ex.Message}",
                //                "WTLayoutManager",
                //                MessageBoxButton.OK,
                //                MessageBoxImage.Error);
                _messageBoxService.ShowMessage($"Failed to delete folder.\n{ex.Message}", "Error", DialogType.Error);
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
                //MessageBox.Show($"Failed to open folder.\n{ex.Message}",
                //                "WTLayoutManager",
                //                MessageBoxButton.OK,
                //                MessageBoxImage.Error);
                _messageBoxService.ShowMessage($"Failed to open folder.\n{ex.Message}", "Error", DialogType.Error);
            }
        }

        private void ExecuteEditFolderCommand(object? parameter)
        {
            // You need this property in your FolderViewModel
        }

        private void ExecuteConfirmEditCommand(object? parameter)
        {
            // You need this property in your FolderViewModel
        }
        private void ExecuteCancelEditCommand(object? parameter)
        {
            // You need this property in your FolderViewModel
        }
    }
}
