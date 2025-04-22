using WTLayoutManager.ViewModels;

/// <summary>
/// Represents a file model with metadata and associated profiles and tab states.
/// </summary>
/// <remarks>
/// Contains information about a file including its name, last modification time, size,
/// and optional references to settings and state view models.
/// </remarks>
namespace WTLayoutManager.Models
{
    public class FileModel
    {
        public string? FileName { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
        public SettingsJsonTooltipViewModel? Profiles { get; set; }
        public StateJsonTooltipViewModel? TabStates { get; set; }
    }
}
