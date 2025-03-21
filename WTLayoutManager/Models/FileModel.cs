using WTLayoutManager.ViewModels;

namespace WTLayoutManager.Models
{
    public class FileModel
    {
        public string FileName { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
        public SettingsJsonTooltipViewModel Profiles { get; set; }
        public StateJsonTooltipViewModel TabStates { get; set; }
    }
}
