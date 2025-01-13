using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using WTLayoutManager.Services;

namespace WTLayoutManager.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ITerminalService _terminalService;
        Dictionary<string, TerminalInfo>? _terminalDict;
        private string _searchText;
        private object _selectedTerminal;

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
            LoadFolders();
        }

        public ObservableCollection<object> Terminals { get; }

        public object SelectedTerminal
        {
            get => _selectedTerminal;
            set
            {
                if (_selectedTerminal != value)
                {
                    _selectedTerminal = value;
                    OnPropertyChanged();
                    // Potentially reload folders if needed
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
            // Example: read directories from "C:\Users\<User>\AppData\Local\Packages\WTLayoutManager\LocalStates"
            // For each folder, create a FolderViewModel and add to Folders
        }

        private IEnumerable<object> LoadInstalledTerminals()
        {
            // Return a list or array of some objects representing installed terminals
            // e.g. Terminal name, path, or ID
            //return new List<object> { "Windows Terminal (Default)", "Another Terminal" };
            _terminalDict = _terminalService.FindAllTerminals();
            return _terminalDict.Select(kvp => new
            {
                ImageSource = kvp.Value.LogoAbsoluteUri,
                DisplayName = kvp.Key
            }).ToList();
        }
    }
}
