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
        public string ImageSource { get; set; }
        public string DisplayName { get; set; }
    }

    public class MainViewModel : BaseViewModel
    {
        private readonly ITerminalService _terminalService;
        Dictionary<string, TerminalInfo>? _terminalDict;
        private string _searchText;
        private TerminalListItem _selectedTerminal;
        private readonly IMessageBoxService _messageBoxService;

        public MainViewModel(IMessageBoxService messageBoxService)
        {
            _messageBoxService = messageBoxService;
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

        // Whenever folders are added, subscribe to their PropertyChanged so we can update TerminalsComboBoxEnabled.
        private void Folders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is FolderViewModel fvm)
                    {
                        fvm.PropertyChanged += FolderViewModel_PropertyChanged;
                    }
                }
            }
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is FolderViewModel fvm)
                    {
                        fvm.PropertyChanged -= FolderViewModel_PropertyChanged;
                    }
                }
            }
            // Update the TerminalsComboBoxEnabled property when folders change.
            OnPropertyChanged(nameof(TerminalsComboBoxEnabled));
        }

        private void FolderViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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

        public TerminalListItem SelectedTerminal
        {
            get => _selectedTerminal;
            set
            {
                if (_selectedTerminal != value)
                {
                    _selectedTerminal = value;
                    OnPropertyChanged();
                    LoadFolders();
                }
            }
        }

        public ObservableCollection<FolderViewModel> Folders { get; }
        public ICollectionView FoldersView { get; }

        public string SearchText
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

        private void ExecuteClearSearchCommand(object? parameter)
        {
            SearchText = string.Empty;
        }

        public Dictionary<string, TerminalInfo>? TerminalDict { get => _terminalDict; set => _terminalDict = value; }

        private bool FilterFolders(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            if (item is FolderViewModel fvm)
            {
                return fvm.Name
                          .ToLowerInvariant()
                          .Contains(SearchText.ToLowerInvariant());
            }
            return false;
        }

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
            if (!_terminalDict.TryGetValue(key, out TerminalInfo terminalInfo))
                return; // no match

            // 5) Suppose you have "folders" or some property inside TerminalInfo that
            //    lists the "LocalState" folders. We iterate over them and create FolderViewModels.
            //    If TerminalInfo doesn't have that, adapt the logic accordingly.
            //    This is just an example:

            LoadFoldersForTerminal(terminalInfo);
        }

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

        public static Dictionary<string, string> MapProfilesToIcons(ObservableCollection<ProfileInfo> profiles)
        {
            // Assumes each profile's ProfileName is unique.
            return profiles
                .GroupBy(p => p.ProfileName)
                .ToDictionary(g => g.Key, g => g.First().IconPath);
        }

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

        private List<string> FindLocalStateFolders(TerminalInfo info)
        {
            // Example stub: however you discover "LocalState" folders
            // For instance, you could look in info.InstalledLocationPath,
            // or maybe you stored them in info.LocalStateFiles, etc.

            // For now, assume info.LocalStateFiles has the full paths
            return info.LocalStateFiles;
        }

        private IEnumerable<TerminalListItem> LoadInstalledTerminals()
        {
            _terminalDict = _terminalService.FindAllTerminals();
            if (_terminalDict != null)
            {
                return _terminalDict.Select(kvp => new TerminalListItem
                {
                    ImageSource = kvp.Value.LogoAbsoluteUri,
                    DisplayName = kvp.Key
                }).ToList();
            }
            else 
            { 
                return Enumerable.Empty<TerminalListItem>();
            }
        }
    }
}
