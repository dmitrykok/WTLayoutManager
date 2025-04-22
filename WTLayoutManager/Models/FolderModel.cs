/// <summary>
/// Represents a folder model with properties for name, path, default status, last run time, and associated files.
/// </summary>
/// <remarks>
/// This model is used to store information about a folder, including its metadata and a collection of files.
/// </remarks>
namespace WTLayoutManager.Models
{
    /// <summary>
    /// Represents a folder with its metadata and associated files.
    /// </summary>
    /// <remarks>
    /// Contains properties for folder name, path, default status, last run time, and a collection of files.
    /// </remarks>
    public class FolderModel
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public bool IsDefault { get; set; }
        public DateTime? LastRun { get; set; }
        // Possibly store the sub-items here:
        public List<FileModel>? Files { get; set; }
    }
}
