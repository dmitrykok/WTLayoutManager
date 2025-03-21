using System.Collections.ObjectModel;

namespace WTLayoutManager.ViewModels
{
    public class StateJsonTooltipViewModel
    {
        public ObservableCollection<TabStateViewModel> TabStates { get; set; } = new ObservableCollection<TabStateViewModel>();
    }
}
