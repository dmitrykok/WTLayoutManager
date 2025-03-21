namespace WTLayoutManager.ViewModels
{
    public class PaneViewModel
    {
        public string ProfileName { get; set; }
        public string Icon { get; set; }
        // Grid positioning information.
        public int GridRow { get; set; } = 0;
        public int GridColumn { get; set; } = 0;
        public int GridRowSpan { get; set; } = 1;
        public int GridColumnSpan { get; set; } = 1;
        // Store the split direction if applicable.
        public string SplitDirection { get; set; }
    }
}
