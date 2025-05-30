using System.Collections.ObjectModel;

namespace WTLayoutManager.ViewModels
{
    /// <summary>
    /// Represents a view model for state JSON tooltips.
    /// </summary>
    public class StateJsonTooltipViewModel
    {
        /// <summary>
        /// Gets or sets the collection of tab states.
        /// </summary>
        /// <value>
        /// An observable collection of <see cref="TabStateViewModel"/> instances.
        /// </value>
        public ObservableCollection<TabStateViewModel> TabStates { get; set; } = new ObservableCollection<TabStateViewModel>();
    }
}
