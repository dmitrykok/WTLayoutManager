using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
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

        public MainViewModel()
        {
            // Load installed terminals
            _terminalService = new TerminalService();
            Terminals = new ObservableCollection<object>(LoadInstalledTerminals());

            // Create the main collection of FolderViewModels
            Folders = new ObservableCollection<FolderViewModel>();

            // If you want a CollectionView for filtering
            FoldersView = CollectionViewSource.GetDefaultView(Folders);
            FoldersView.Filter = FilterFolders;

            // Example of loading the folders
            // LoadFolders();
        }

        public ObservableCollection<object> Terminals { get; }

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
            var selectedTerminal = _terminalDict[SelectedTerminal.DisplayName];
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
