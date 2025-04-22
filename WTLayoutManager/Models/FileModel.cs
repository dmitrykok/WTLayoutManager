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
    /// <summary>
    /// Represents a file with its metadata and associated profiles and tab states.
    /// </summary>
    /// <remarks>
    /// Contains properties for file details including name, last modification time, size,
    /// and optional references to settings and state view models.
    /// </remarks>
    public class FileModel
    {
        public string? FileName { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
        public SettingsJsonTooltipViewModel? Profiles { get; set; }
        public StateJsonTooltipViewModel? TabStates { get; set; }
    }
}
