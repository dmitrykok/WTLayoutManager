namespace WTLayoutManager.Models
{
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
