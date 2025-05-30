using System.Collections.ObjectModel;
using WTLayoutManager.Models;

namespace WTLayoutManager.ViewModels
{
    /// <summary>
    /// Represents a view model for settings JSON tooltips.
    /// </summary>
    public class SettingsJsonTooltipViewModel
    {
        /// <summary>
        /// Gets or sets the collection of profile information.
        /// </summary>
        /// <value>A collection of <see cref="ProfileInfo"/> objects.</value>
        public ObservableCollection<ProfileInfo> Profiles { get; set; } = new();
    }
}
