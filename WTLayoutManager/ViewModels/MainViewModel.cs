using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using System.Windows.Input;
using WTLayoutManager.Models;
using WTLayoutManager.Services;

namespace WTLayoutManager.ViewModels
{
    public class TerminalListItem
    {
        public string? ImageSource { get; set; }
        public string? DisplayName { get; set; }
        public string? Version { get; set; }
    }

    public class MainViewModel : BaseViewModel
    {
        private readonly ITerminalService _terminalService;
        Dictionary<string, TerminalInfo>? _terminalDict;
        private string? _searchText;
        private TerminalListItem? _selectedTerminal;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// 
        /// This constructor loads installed terminals, creates a collection of FolderViewModels, 
        /// and sets up commands for clearing search and reloading folders.
        /// 
        /// Parameters:
        ///     messageBoxService (IMessageBoxService): The message box service used for displaying messages.
        /// 
        /// Returns:
        ///     None
        /// </summary>
        public MainViewModel(IMessageBoxService messageBoxService)
            : base(messageBoxService)
        {
            // Load installed terminals
            _terminalService = new TerminalService(_messageBoxService);
            Terminals = new ObservableCollection<TerminalListItem>(LoadInstalledTerminals());

            // Create the main collection of FolderViewModels
            Folders = new ObservableCollection<FolderViewModel>();
            Folders.CollectionChanged += Folders_CollectionChanged;

            // If you want a CollectionView for filtering
            FoldersView = CollectionViewSource.GetDefaultView(Folders);
            FoldersView.Filter = FilterFolders;

            // Example of loading the folders
            // LoadFolders();
            ClearSearchCommand = new RelayCommand(ExecuteClearSearchCommand);
            ReloadFoldersCommand = new RelayCommand(_ => LoadFolders());
        }


        /// <summary>
        /// Handles the CollectionChanged event of the Folders collection.
        /// 
        /// This method is responsible for attaching and detaching PropertyChanged event handlers to FolderViewModel instances
        /// when they are added or removed from the Folders collection. It also raises a PropertyChanged event for the
        /// TerminalsComboBoxEnabled property when the Folders collection changes.
        /// 
        /// Parameters:
        ///     sender (object): The source of the event.
        ///     e (NotifyCollectionChangedEventArgs): The event arguments, containing information about the change.
        /// </summary>
        private void Folders_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Check if new items were added to the collection
            if (e.NewItems != null)
            {
                // Iterate over the new items and attach PropertyChanged event handlers
                foreach (var item in e.NewItems)
                {
                    if (item is FolderViewModel fvm)
                    {
                        // Attach PropertyChanged event handler to the FolderViewModel instance
                        fvm.PropertyChanged += FolderViewModel_PropertyChanged;
                    }
                }
            }

            // Check if old items were removed from the collection
            if (e.OldItems != null)
            {
                // Iterate over the old items and detach PropertyChanged event handlers
                foreach (var item in e.OldItems)
                {
                    if (item is FolderViewModel fvm)
                    {
                        // Detach PropertyChanged event handler from the FolderViewModel instance
                        fvm.PropertyChanged -= FolderViewModel_PropertyChanged;
                    }
                }
            }

            // Update the TerminalsComboBoxEnabled property when folders change
            OnPropertyChanged(nameof(TerminalsComboBoxEnabled));
        }

        /// <summary>
        /// Handles the PropertyChanged event of a FolderViewModel instance.
        /// 
        /// This method checks if the changed property is either CanRunTerminal or CanRunTerminalAs,
        /// and if so, raises a PropertyChanged event for the TerminalsComboBoxEnabled property.
        /// 
        /// Parameters:
        ///     sender (object): The source of the event.
        ///     e (PropertyChangedEventArgs): The event arguments, containing the name of the changed property.
        /// 
        /// Returns:
        ///     None
        /// </summary>
        private void FolderViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FolderViewModel.CanRunTerminal) ||
                e.PropertyName == nameof(FolderViewModel.CanRunTerminalAs))
            {
                OnPropertyChanged(nameof(TerminalsComboBoxEnabled));
            }
        }

        // Property to disable the ComboBox if any folder is running a terminal.
        public bool TerminalsComboBoxEnabled => !Folders.Any(folder => !folder.CanRunTerminal || !folder.CanRunTerminalAs);


        public ObservableCollection<TerminalListItem> Terminals { get; }

        public TerminalListItem? SelectedTerminal
        {
            get => _selectedTerminal;
            set
            {
                if (_selectedTerminal != value)
                {
                    _selectedTerminal = value;
                    OnPropertyChanged();
                    OnPropertyChanged("SelectedTerminalVersion");
                    LoadFolders();
                }
            }
        }

        public string? SelectedTerminalVersion
        {
            get => _selectedTerminal?.Version ?? "Choose Terminal...";
        }

        public ObservableCollection<FolderViewModel> Folders { get; }
        public ICollectionView FoldersView { get; }

        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    FoldersView.Refresh();
                }
            }
        }

        public ICommand ClearSearchCommand { get; }
        public ICommand ReloadFoldersCommand { get; }

        /// <summary>
        /// Clears the search text.
        /// </summary>
        /// <param name="parameter">Ignored parameter.</param>
        /// <remarks>
        /// This method resets the SearchText property to an empty string.
        /// </remarks>
        private void ExecuteClearSearchCommand(object? parameter)
        {
            SearchText = string.Empty;
        }


        public Dictionary<string, TerminalInfo>? TerminalDict { get => _terminalDict; set => _terminalDict = value; }

        /// <summary>
        /// Filters a collection of folders based on a search text.
        /// 
        /// This function checks if the search text is null or whitespace, and if so, returns true.
        /// Otherwise, it checks if the item is a FolderViewModel and if its name contains the search text.
        /// 
        /// Parameters:
        ///     item (object): The item to filter, expected to be a FolderViewModel.
        /// 
        /// Returns:
        ///     bool: True if the item matches the search text, false otherwise.
        /// </summary>
        private bool FilterFolders(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            if (item is FolderViewModel fvm)
            {
                if (fvm.Name != null)
                {
                    return fvm.Name
                              .ToLowerInvariant()
                              .Contains(SearchText.ToLowerInvariant());
                }
            }
            return false;
        }

        /// <summary>
        /// Loads the folders for the currently selected terminal.
        /// 
        /// This function clears any existing folder view-models, checks for a valid terminal dictionary and selection,
        /// retrieves the TerminalInfo from the dictionary, and then loads the folders for the terminal.
        /// 
        /// Parameters: None
        /// 
        /// Returns: None
        /// </summary>
        private void LoadFolders()
        {
            // 1) Clear existing folder view-models
            Folders.Clear();

            // 2) Guard clauses: if dictionary or selection is null, do nothing
            if (_terminalDict == null || _selectedTerminal == null)
                return;

            // 3) Cast or access the DisplayName from SelectedTerminal
            //    (assuming you changed SelectedTerminal to be strongly typed with .DisplayName)
            var key = _selectedTerminal.DisplayName;
            if (string.IsNullOrEmpty(key))
                return;

            // 4) Get the TerminalInfo from the dictionary
            if (!_terminalDict.TryGetValue(key, out var terminalInfo))
                return; // no match

            // 5) Suppose you have "folders" or some property inside TerminalInfo that
            //    lists the "LocalState" folders. We iterate over them and create FolderViewModels.
            //    If TerminalInfo doesn't have that, adapt the logic accordingly.
            //    This is just an example:

            LoadFoldersForTerminal(terminalInfo);
        }

        /// <summary>
        /// Loads folders for a specific terminal, including the default LocalState folder and any custom LocalState folders.
        /// 
        /// This function clears any existing items from the Folders collection before re-populating it with the default and custom folders.
        /// 
        /// Parameters:
        ///     info (TerminalInfo): The terminal information used to determine the folder paths.
        /// 
        /// Returns:
        ///     None
        /// </summary>
        private void LoadFoldersForTerminal(TerminalInfo info)
        {
            // Clear any existing items from Folders before re-populating
            Folders.Clear();

            // 1) Default LocalState folder path
            //    Example: %LOCALAPPDATA%\Packages\<familyName>\LocalState
            string defaultFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages",
                info.FamilyName,
                "LocalState");

            // Create a FolderModel for the default folder
            var defaultFolderModel = CreateFolderModel(
                folderPath: defaultFolderPath,
                folderName: "LocalState (Default)", // or just "LocalState"
                isDefault: true);

            // Convert the FolderModel into FolderViewModel and add to Folders collection
            Folders.Add(new FolderViewModel(defaultFolderModel, this, _messageBoxService));

            // 2) Virtual/“custom” LocalState folders
            //    Located under: %LOCALAPPDATA%\WTLayoutManager\<familyName>
            //    Each subfolder is effectively a "LocalState" copy
            string customBasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WTLayoutManager",
                info.FamilyName);

            if (Directory.Exists(customBasePath))
            {
                // For each subfolder, treat it as a "LocalState" copy
                foreach (string subDir in Directory.GetDirectories(customBasePath))
                {
                    // We'll use the subfolder name for display, or you might store a separate metadata file
                    string folderName = Path.GetFileName(subDir);

                    var customFolderModel = CreateFolderModel(
                        folderPath: subDir,
                        folderName: folderName,
                        isDefault: false);

                    // Add
                    Folders.Add(new FolderViewModel(customFolderModel, this, _messageBoxService));
                }
            }

            // Done. Now Folders contains the default folder + any custom copies.
            // The DataGrid or whatever UI is bound to FoldersView (GetDefaultView(Folders)) 
            // and should refresh automatically.
        }

        /// <summary>
        /// Maps a collection of profiles to their corresponding icons.
        /// 
        /// This function takes an ObservableCollection of ProfileInfo objects as input, 
        /// groups them by their ProfileName, and returns a Dictionary where each key is a 
        /// ProfileName and the corresponding value is the IconPath of the first profile 
        /// with that name.
        /// 
        /// Parameters:
        /// profiles (ObservableCollection<ProfileInfo>): The collection of profiles to map.
        /// 
        /// Returns:
        /// Dictionary<string, string>: A dictionary mapping ProfileNames to IconPaths.
        /// </summary>
        public static Dictionary<string, string> MapProfilesToIcons(ObservableCollection<ProfileInfo> profiles)
        {
            // Assumes each profile's ProfileName is unique.
            return profiles
                .GroupBy(p => p.ProfileName!)
                .ToDictionary(g => g.Key, g => g.First().IconPath!);
        }

        /// <summary>
        /// Creates a FolderModel instance based on the provided folder path, name, and default status.
        /// 
        /// This function populates the FolderModel with a list of FileModel instances, each representing a file 
        /// with a specific name (settings.json, state.json, elevated-state.json) found in the specified folder path.
        /// 
        /// The function also attempts to parse profile information and state data from the files, 
        /// and assigns the last modified time of the state.json file as the FolderModel's LastRun time if applicable.
        /// 
        /// Parameters:
        ///     folderPath (string): The path to the folder.
        ///     folderName (string): The name of the folder.
        ///     isDefault (bool): A flag indicating whether the folder is the default one.
        /// 
        /// Returns:
        ///     FolderModel: An instance of FolderModel representing the specified folder.
        /// </summary>
        private FolderModel CreateFolderModel(string folderPath, string folderName, bool isDefault)
        {
            var model = new FolderModel
            {
                Name = folderName,
                Path = folderPath,
                IsDefault = isDefault,
                Files = new List<FileModel>() // we’ll populate below
            };

            // We only care about these 3 possible files:
            var interestingFiles = new[] { "settings.json", "state.json", "elevated-state.json" };

            // Populate the list of FileModel if files exist
            var mapProfilesToIcons = new Dictionary<string, string>();
            foreach (string fileName in interestingFiles)
            {
                string fullPath = Path.Combine(folderPath, fileName);
                if (File.Exists(fullPath))
                {
                    var fi = new FileInfo(fullPath);
                    var tooltipProfiles = new ObservableCollection<ProfileInfo>(SettingsJsonParser.GetProfileInfos(fullPath));
                    if (mapProfilesToIcons.Count == 0)
                        mapProfilesToIcons = MapProfilesToIcons(tooltipProfiles);
                    var stateTooltipVm = StateJsonParser.ParseState(fullPath, mapProfilesToIcons);
                    var tooltipVm = new SettingsJsonTooltipViewModel
                    {
                        Profiles = tooltipProfiles
                    };
                    model.Files.Add(new FileModel
                    {
                        FileName = fi.Name,
                        LastModified = fi.LastWriteTime,
                        Size = fi.Length,
                        Profiles = tooltipVm,
                        TabStates = stateTooltipVm
                    });
                    if (model.LastRun == null && fileName == "state.json")
                    {
                        model.LastRun = fi.LastWriteTime;
                    }
                }
            }

            // Optionally, fill model.LastRun if you have logic to track that
            // model.LastRun = ...

            return model;
        }

        /// <summary>
        /// Collapses all FolderViewModels in the Folders collection except for the specified keepOpen instance.
        /// </summary>
        /// <param name="keepOpen">The FolderViewModel instance that should remain expanded.</param>
        public void CollapseAllExcept(FolderViewModel keepOpen)
        {
            foreach (var fvm in Folders)
            {
                if (fvm != keepOpen)
                {
                    fvm.IsExpanded = false;
                }
            }
        }

        /// <summary>
        /// Finds the local state folders for a given terminal.
        /// 
        /// This function takes a TerminalInfo object as a parameter and returns a list of strings representing the full paths to the local state folders.
        /// 
        /// The function currently assumes that the full paths to the local state folders are stored in the TerminalInfo object's LocalStateFiles property.
        /// </summary>
        /// <param name="info">A TerminalInfo object containing information about the terminal.</param>
        /// <returns>A list of strings representing the full paths to the local state folders.</returns>
        private List<string> FindLocalStateFolders(TerminalInfo info)
        {
            // Example stub: however you discover "LocalState" folders
            // For instance, you could look in info.InstalledLocationPath,
            // or maybe you stored them in info.LocalStateFiles, etc.

            // For now, assume info.LocalStateFiles has the full paths
            return info.LocalStateFiles;
        }


        /// <summary>
        /// Finds the local state folders for a given terminal.
        /// 
        /// This function takes a TerminalInfo object as a parameter and returns a list of strings representing the full paths to the local state folders.
        /// 
        /// The function currently assumes that the full paths to the local state folders are stored in the TerminalInfo object's LocalStateFiles property.
        /// </summary>
        /// <param name="info">A TerminalInfo object containing information about the terminal.</param>
        /// <returns>A list of strings representing the full paths to the local state folders.</returns>
        private IEnumerable<TerminalListItem> LoadInstalledTerminals()
        {
            _terminalDict = _terminalService.FindAllTerminals();
            if (_terminalDict != null)
            {
                return _terminalDict.Select(kvp => new TerminalListItem
                {
                    ImageSource = kvp.Value.LogoAbsoluteUri,
                    DisplayName = kvp.Key,
                    Version = kvp.Value.Version.ToString(),
                }).ToList();
            }
            else
            {
                return Enumerable.Empty<TerminalListItem>();
            }
        }
    }
}
