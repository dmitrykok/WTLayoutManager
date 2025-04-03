namespace WTLayoutManager.ViewModels
{
    public class PaneViewModel
    {
        public string? ProfileName { get; set; }
        public string? Icon { get; set; }
        // Geometry in normalized coordinates
        public double X { get; set; }      // Left position
        public double Y { get; set; }      // Top position
        public double Width { get; set; }
        public double Height { get; set; }

        // Grid placement (computed later)
        public int GridRow { get; set; } = 0;
        public int GridColumn { get; set; } = 0;
        public int GridRowSpan { get; set; } = 1;
        public int GridColumnSpan { get; set; } = 1;

        // Store the split direction (for reference if needed)
        public string? SplitDirection { get; set; }
    }
}
