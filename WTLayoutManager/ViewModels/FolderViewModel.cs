using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Input;
using WTLayoutManager.Models;
using WTLayoutManager.Services;

/// <summary>
/// Represents a view model for managing Windows Terminal local state folders, providing functionality for running, duplicating, deleting, and interacting with terminal folders.
/// </summary>
/// <remarks>
/// This view model handles operations such as launching terminals with specific local state configurations, creating folder duplicates,
/// managing folder properties, and providing commands for various folder-related interactions.
/// </remarks>
namespace WTLayoutManager.ViewModels
{
    /// <summary>
    /// Represents a view model for managing Windows Terminal local state folders, providing functionality for running, duplicating, deleting, and interacting with terminal folders.
    /// </summary>
    /// <remarks>
    /// This view model handles operations such as launching terminals with specific local state configurations, creating folder duplicates,
    /// managing folder properties, and providing commands for various folder-related interactions.
    /// </remarks>
    public class FolderViewModel : BaseViewModel
    {
        private readonly FolderModel _folder;
        private readonly MainViewModel _parentViewModel;
        private readonly string _localAppRoot;
        private readonly string _localAppBin;
        private readonly string _hookFileName;
        private readonly string _srcHookPath;
        private readonly string _dstHookPath;
        private readonly Dictionary<string, Task<int>> _runningTerminals = new Dictionary<string, Task<int>>();
        private readonly Dictionary<string, Task<int>> _runningTerminalsAs = new Dictionary<string, Task<int>>();

        /// <summary>
        /// Represents a folder with its associated files and settings, including operations for running, duplicating, deleting, and editing the folder.
        /// </summary>
        /// <remarks>
        /// This view model is used to manage a folder with its associated files and settings, including running the terminal, duplicating or deleting the folder,
        /// and editing the folder's name and path. The view model also handles user interactions such as confirmation dialogs and editing folder properties.
        /// </remarks>
        public FolderViewModel(FolderModel folder, MainViewModel parentViewModel, IMessageBoxService messageBoxService)
            : base(messageBoxService)
        {
            _folder = folder;
            _parentViewModel = parentViewModel;
            _localAppRoot = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WTLayoutManager");
            _localAppBin = System.IO.Path.Combine(_localAppRoot, "bin");
            if (!Directory.Exists(_localAppBin))
            {
                Directory.CreateDirectory(_localAppBin);
            }

            _hookFileName = $"WTLocalStateHook{ProcessBitness}.dll";
            _srcHookPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _hookFileName);
            _dstHookPath = System.IO.Path.Combine(_localAppBin, _hookFileName);

            if (!File.Exists(_dstHookPath) || !FilesEqual(_srcHookPath, _dstHookPath))
            {
                string tmp = System.IO.Path.Combine(_localAppBin, $"{Guid.NewGuid()}.tmp");
                try
                {
                    File.Copy(_srcHookPath, tmp, overwrite: true);
                    if (!File.Exists(_dstHookPath))
                    {
                        File.Move(tmp, _dstHookPath, overwrite: true);
                    }
                    else
                    {
                        File.Replace(tmp, _dstHookPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
                    }
                }
                finally
                {
                    if (File.Exists(tmp))
                    {
                        try { File.Delete(tmp); } catch { /* ignore cleanup errors */ }
                    }
                }
            }

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

        public bool CanRunTerminal => Path is { Length: > 0 } && !_runningTerminals.ContainsKey(Path);
        public bool CanRunTerminalAs => Path is { Length: > 0 } && !_runningTerminalsAs.ContainsKey(Path);

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
        private ICommand? _toggleExpandCollapseCommand;
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

        public string? Name
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

        public string? Path
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

        public string ProcessBitness
        {
            get => Environment.Is64BitProcess ? "64" : "32";
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

        public List<FileModel>? Files => _folder.Files; // or new ObservableCollection if needed

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

        /// <summary>
        /// Validates that a given LocalState folder path is not null or empty.
        /// If the path is invalid, an error message is shown and false is returned.
        /// </summary>
        /// <param name="path">The LocalState folder path to validate</param>
        /// <returns>true if the path is valid, false otherwise</returns>
        private bool ValidateFolderPath([NotNullWhen(true)] string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                _messageBoxService.ShowMessage("LocalState folder path is null.", "Error", DialogType.Error);
                return false;
            }
            return true;
        }

        private bool TerminalHasLocalStateParam(TerminalInfo terminalInfo)
        {
            return (!IsDefault && terminalInfo.Version >= "1.24.53104.5" && terminalInfo.Publisher == "CN=Dm17tryK");
        }

        /// <summary>
        /// Builds the command line for running a terminal executable with the given file name,
        /// adding the "--localstate" option if the terminal version is at least 1.25.53104.5.
        /// </summary>
        /// <param name="terminalInfo">The terminal information, including the version</param>
        /// <param name="fileName">The file name of the terminal executable</param>
        /// <returns>The command line to run the terminal executable</returns>
        private string BuildCommandLine(TerminalInfo terminalInfo, string fileName)
        {
            var commandLine = $"\"{fileName}\"";
            //if (TerminalHasLocalStateParam(terminalInfo))
            //{
            //    commandLine += $" --localstate \"{Path}\"";
            //}
            return commandLine;
        }

        /// <summary>
        /// Builds an environment block for the terminal executable, setting the <c>WT_BASE_SETTINGS_PATH</c>
        /// environment variable to the path of the folder when the folder is not the default one and the
        /// terminal version is at least 1.24.53104.5.
        /// </summary>
        /// <param name="terminalInfo">The terminal information, including the version</param>
        /// <returns>The environment block to pass to the terminal executable</returns>
        private string BuildEnvironmentBlock(TerminalInfo terminalInfo)
        {
            string defaultFolderPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages",
                terminalInfo.FamilyName,
                "LocalState");

            //defaultFolderPath = "C:\\Users\\dmitr\\AppData\\Local\\Microsoft\\Windows Terminal";

            string envBlock = string.Empty;
            //if (TerminalHasLocalStateParam(terminalInfo))
            //{
            //    // Single-null terminators per variable, ending with a double-null terminator.
            //    envBlock += $";WT_BASE_SETTINGS_PATH={Path}";
            //}
            envBlock += $";WT_DEFAULT_LOCALSTATE={defaultFolderPath}";
            envBlock += $";WT_REDIRECT_LOCALSTATE={Path}";
            envBlock += $";WT_HOOK_DLL_PATH={_dstHookPath}";
            return envBlock.TrimStart(';');
        }

        /// <summary>
        /// Builds the path to the terminal executable given the terminal information.
        /// Uses "WindowsTerminal.exe" as the default file name, and falls back to "wtd.exe" if that is not found.
        /// </summary>
        /// <param name="terminalInfo">The terminal information, including the installation location path</param>
        /// <returns>The path to the terminal executable</returns>
        private string BuildTerminalPath(TerminalInfo terminalInfo)
        {
            //return "C:\\Users\\dmitr\\src\\terminal\\bin\\x64\\Debug\\WindowsTerminal\\WindowsTerminal.exe";
            var fileName = System.IO.Path.Combine(terminalInfo.InstalledLocationPath, "wt.exe");
            if (!File.Exists(fileName))
            {
                fileName = System.IO.Path.Combine(terminalInfo.InstalledLocationPath, "wtd.exe");
            }

            return fileName;
        }

        private static bool FilesEqual(string a, string b)
        {
            var infoA = FileVersionInfo.GetVersionInfo(a);
            var infoB = FileVersionInfo.GetVersionInfo(b);

            if (infoA.FileVersion != infoB.FileVersion ||
                infoA.FileSize() != infoB.FileSize()) // extension method below
                return false;        // version changed → must refresh

            // Same version: fall back to a fast hash if the files are small.
            // (Avoid for very large DLLs.)
            using var sha = SHA256.Create();
            return sha.ComputeHash(File.ReadAllBytes(a))
                      .SequenceEqual(sha.ComputeHash(File.ReadAllBytes(b)));
        }

        /// <summary>
        /// Executes the terminal executable associated with the selected terminal,
        /// passing the folder path as the "--localstate" option if the terminal version is at least 1.25.53104.5.
        /// Also sets the <c>WT_BASE_SETTINGS_PATH</c> environment variable to the path of the folder
        /// when the folder is not the default one and the terminal version is at least 1.24.53104.5.
        /// </summary>
        /// <param name="runningTerminals">The dictionary of running terminals, keyed by the folder path</param>
        /// <param name="alreadyRunningMessage">The message to show if the terminal is already running</param>
        /// <param name="propertyName">The name of the property to raise the PropertyChanged event for</param>
        /// <param name="launchProcess">The function to launch the terminal executable</param>
        /// <returns>A task that completes when the terminal executable exits</returns>
        private async Task ExecuteTerminalAsync(
            Dictionary<string, Task<int>> runningTerminals,
            string alreadyRunningMessage,
            string propertyName,
            Func<string, string, string, string, Task<int>> launchProcess
        )
        {
            if (!ValidateFolderPath(Path))
                return;

            if (runningTerminals.ContainsKey(Path))
            {
                _messageBoxService.ShowMessage(alreadyRunningMessage, "Warning", DialogType.Warning);
                return;
            }

            var key = _parentViewModel.SelectedTerminal?.DisplayName;
            if (key != null && _parentViewModel.TerminalDict?.TryGetValue(key, out var terminalInfo) == true)
            {
                var fileName = BuildTerminalPath(terminalInfo);
                var commandLine = BuildCommandLine(terminalInfo, fileName);
                var envBlock = BuildEnvironmentBlock(terminalInfo);
                var hookPath = _dstHookPath;

                if (!File.Exists(fileName))
                {
                    _messageBoxService.ShowMessage($"File not found.\n{fileName}", "Error", DialogType.Error);
                    return;
                }

                if (!File.Exists(hookPath))
                {
                    _messageBoxService.ShowMessage($"File not found.\n{hookPath}", "Error", DialogType.Error);
                    return;
                }

                Task<int> launchTask = launchProcess(
                    fileName,
                    commandLine,
                    envBlock,
                    hookPath
                );
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

        /// <summary>
        /// Executes the terminal executable associated with the selected terminal,
        /// passing the folder path as the "--localstate" option if the terminal version is at least 1.25.53104.5.
        /// Also sets the <c>WT_BASE_SETTINGS_PATH</c> environment variable to the path of the folder
        /// when the folder is not the default one and the terminal version is at least 1.24.53104.5.
        /// </summary>
        private async Task ExecuteRunAsync()
        {
            if (!ValidateFolderPath(Path))
                return;

            try
            {
                await ExecuteTerminalAsync(
                    _runningTerminals,
                    "Terminal is already running for this local state.",
                    nameof(CanRunTerminal),
                    (fileName, commandLine, envBlock, hookPath) => Task.Run(() => ProcessLauncher.LaunchProcess(
                        fileName,
                        commandLine,
                        envBlock,
                        hookPath
                        )
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

        /// <summary>
        /// Executes the terminal executable associated with the selected terminal,
        /// passing the folder path as the "--localstate" option if the terminal version is at least 1.25.53104.5.
        /// Also sets the <c>WT_BASE_SETTINGS_PATH</c> environment variable to the path of the folder
        /// when the folder is not the default one and the terminal version is at least 1.24.53104.5.
        /// Requests elevation (UAC prompt) before launching the terminal.
        /// </summary>
        private async Task ExecuteRunAsAsync()
        {
            if (!ValidateFolderPath(Path))
                return;

            try
            {
                await ExecuteTerminalAsync(
                    _runningTerminalsAs,
                    "Terminal Admin is already running for this local state.",
                    nameof(CanRunTerminalAs),
                    (fileName, commandLine, envBlock, hookPath) => Task.Run(() => ProcessLauncher.LaunchProcessElevated(
                        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ElevatedLauncher.exe"),
                        fileName,
                        commandLine,
                        envBlock,
                        hookPath
                        )
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

        /// <summary>
        /// Duplicates the folder at <see cref="Path"/> by copying all files to a new folder
        /// with the same parent directory and a new name, then adds the new folder to the
        /// parent's <see cref="MainViewModel.Folders"/> collection.
        /// Optionally uses the <paramref name="exFolderName"/> if provided.
        /// </summary>
        /// <param name="parameter">Ignored.</param>
        /// <param name="exFolderName">Optional folder name to use for the new folder.</param>
        private void ExecuteDuplicateEx(object? parameter, string? exFolderName)
        {
            if (!ValidateFolderPath(Path))
                return;

            try
            {
                if (_parentViewModel.TerminalDict != null &&
                    _parentViewModel.SelectedTerminal != null)
                {
                    var key = _parentViewModel.SelectedTerminal.DisplayName;
                    if (key != null && _parentViewModel.TerminalDict.TryGetValue(key, out var terminalInfo))
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

        private static readonly HashSet<string> _filesToCopy = new(StringComparer.OrdinalIgnoreCase) { "settings.json", "state.json" };

        /// <summary>
        /// Recursively copies a directory and all its contents to another directory.
        /// </summary>
        /// <param name="sourceDir">The source directory to copy from.</param>
        /// <param name="destDir">The destination directory to copy to.</param>
        private void DirectoryCopy(string sourceDir, string destDir)
        {
            var dirInfo = new DirectoryInfo(sourceDir);
            if (!dirInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
            }

            Directory.CreateDirectory(destDir);

            // Copy files
            //foreach (FileInfo file in dirInfo.GetFiles())
            foreach (FileInfo file in dirInfo.EnumerateFiles())
            {
                if (!_filesToCopy.Contains(file.Name))
                    continue;                       // skip everything else

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


        /// <summary>
        /// Deletes a folder and all its contents from disk and from the parent view model's Folders collection.
        /// </summary>
        /// <remarks>
        /// If the folder is the default LocalState folder, it will not be deleted.
        /// If the folder is locked (e.g. Terminal is running), an IOException or UnauthorizedAccessException will be thrown.
        /// </remarks>
        /// <param name="parameter">Ignored.</param>
        private void ExecuteDelete(object? parameter)
        {
            if (!ValidateFolderPath(Path))
                return;

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


        /// <summary>
        /// Opens the folder in the file explorer using the folder path associated with this view model.
        /// </summary>
        /// <param name="parameter">Ignored.</param>
        /// <remarks>
        /// If the operation fails, an error message is displayed using the message box service.
        /// </remarks>
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

        /// <summary>
        /// Initiates the edit operation for the folder associated with this view model.
        /// </summary>
        /// <param name="parameter">Ignored.</param>
        /// <remarks>
        /// This method prepares the folder for editing, typically by enabling or displaying
        /// editing UI elements for the folder's properties.
        /// </remarks>
        private void ExecuteEditFolderCommand(object? parameter)
        {
            // You need this property in your FolderViewModel
        }

        /// <summary>
        /// Confirms the edit operation for the folder associated with this view model.
        /// </summary>
        /// <param name="parameter">Ignored.</param>
        /// <remarks>
        /// This method commits the changes made to the folder's properties during the edit operation.
        /// </remarks>
        private void ExecuteConfirmEditCommand(object? parameter)
        {
            // You need this property in your FolderViewModel
        }
        /// <summary>
        /// Cancels the edit operation for the folder associated with this view model.
        /// </summary>
        /// <param name="parameter">Ignored.</param>
        /// <remarks>
        /// This method discards any changes made to the folder's properties during the edit operation.
        /// </remarks>
        private void ExecuteCancelEditCommand(object? parameter)
        {
            // You need this property in your FolderViewModel
        }
    }
}
