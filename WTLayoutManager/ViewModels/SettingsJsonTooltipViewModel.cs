using System.Collections.ObjectModel;
using WTLayoutManager.Models;

namespace WTLayoutManager.ViewModels
{
    public class SettingsJsonTooltipViewModel
    {
        public ObservableCollection<ProfileInfo> Profiles { get; set; } = new();
    }
}
