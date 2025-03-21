using System.Collections.ObjectModel;

namespace WTLayoutManager.ViewModels
{
    public class TabStateViewModel
    {
        public int GridRows { get; set; } = 1;
        public int GridColumns { get; set; } = 1;
        public ObservableCollection<PaneViewModel> Panes { get; set; } = new ObservableCollection<PaneViewModel>();
    }
}
