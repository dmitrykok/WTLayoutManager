using System.IO;
using System.Text.Json;
using WTLayoutManager.Models;
using WTLayoutManager.ViewModels;

/// <summary>
/// Represents an internal context for managing tab and pane state during layout parsing.
/// </summary>
/// <remarks>
/// Tracks the current tab, focused pane, and maintains a list of tabs for the layout.
/// Used internally by the StateJsonParser to manage state during JSON parsing.
/// </remarks>
namespace WTLayoutManager.Services
{
    public static class StateJsonParser
    {
        /// <summary>
        /// Represents the context for managing tab and pane state during layout parsing.
        /// </summary>
        /// <remarks>
        /// Tracks the current tab, focused pane, and maintains a list of tabs for the layout.
        /// </remarks>
        private class TabContext
        {
            public TabStateViewModel? CurrentTab { get; set; }
            public PaneViewModel? CurrentFocusedPane { get; set; }
            public List<TabStateViewModel> Tabs { get; } = new List<TabStateViewModel>();
        }

        const double tolerance = 0.0001;
        // Define a delegate for the action handler:
        /// <summary>
        /// Defines a delegate for handling tab layout actions, specifying the method signature for action handler functions.
        /// </summary>
        /// <param name="action">The tab layout action to be processed.</param>
        /// <param name="profileIcons">A dictionary of profile names mapped to their corresponding icons.</param>
        /// <param name="context">The current tab context for tracking and modifying tab state.</param>
        private delegate void ActionHandlerDelegate(
            TabLayoutAction action,
            Dictionary<string, string> profileIcons,
            TabContext context);

        // Build a dictionary mapping normalized action names to the handlers:
        /// <summary>
        /// Maps action names to their corresponding handler methods for processing tab layout actions.
        /// Supports case-insensitive action name matching with predefined handlers for various tab and pane operations.
        /// </summary>
        static Dictionary<string, ActionHandlerDelegate> actionHandlers = new Dictionary<string, ActionHandlerDelegate>(StringComparer.OrdinalIgnoreCase)
        {
            { "newtab", HandleNewTab },
            { "splitpane", HandleSplitPane },
            { "focuspane", HandleFocusPane },
            { "movefocus", HandleMoveFocus },
            { "switchtotab", HandleSwitchTab }
        };

        /// <summary>
        /// Handles the "newtab" action by creating a new tab and making it the current tab.
        /// </summary>
        /// <param name="action">The TabLayoutAction containing the action details.</param>
        /// <param name="profileIcons">A dictionary mapping profile names to their corresponding icons.</param>
        /// <param name="context">The current tab context.</param>
        private static void HandleNewTab(
            TabLayoutAction action,
            Dictionary<string, string> profileIcons,
            TabContext context)
        {
            context.CurrentTab = new TabStateViewModel
            {
                TabTitle = action.TabTitle
            };
            var pane = new PaneViewModel
            {
                ProfileName = action.Profile,
                Icon = GetIconForProfile(action.Profile, profileIcons),
                X = 0,
                Y = 0,
                Width = 1,
                Height = 1,
                GridRowSpan = 1,
                GridColumnSpan = 1
            };
            context.CurrentTab.Panes.Add(pane);
            context.CurrentFocusedPane = pane;
            context.Tabs.Add(context.CurrentTab);
        }

        /// <summary>
        /// Handles the "splitpane" action by splitting the currently focused pane
        /// in the specified direction and creating a new pane.
        /// </summary>
        /// <param name="action">The TabLayoutAction containing the action details, including the split direction.</param>
        /// <param name="profileIcons">A dictionary mapping profile names to their corresponding icons.</param>
        /// <param name="context">The current tab context containing the current tab and focused pane.</param>
        /// <remarks>
        /// The function supports splitting the focused pane in four directions: left, right, up, and down.
        /// The newly created pane inherits the profile and icon information from the action. The geometry
        /// of the focused pane is adjusted based on the split direction, and the new pane is added to the
        /// current tab's collection of panes.
        /// </remarks>
        private static void HandleSplitPane(
            TabLayoutAction action,
            Dictionary<string, string> profileIcons,
            TabContext context)
        {
            if (context.CurrentTab != null && context.CurrentFocusedPane != null)
            {
                // Clone the geometry of the currently focused pane.
                double x = context.CurrentFocusedPane.X;
                double y = context.CurrentFocusedPane.Y;
                double w = context.CurrentFocusedPane.Width;
                double h = context.CurrentFocusedPane.Height;
                double newW, newH;
                var splitDir = action.Split?.ToLowerInvariant();
                PaneViewModel newPane = new()
                {
                    ProfileName = action.Profile,
                    Icon = GetIconForProfile(action.Profile, profileIcons),
                    SplitDirection = splitDir
                };

                switch (splitDir)
                {
                    case "left":
                        newW = w / 2;
                        // New pane occupies left half.
                        newPane.X = x;
                        newPane.Y = y;
                        newPane.Width = newW;
                        newPane.Height = h;
                        // Focused pane becomes the right half.
                        context.CurrentFocusedPane.X = x + newW;
                        context.CurrentFocusedPane.Width = newW;
                        break;
                    case "right":
                        newW = w / 2;
                        // New pane occupies right half.
                        newPane.X = x + newW;
                        newPane.Y = y;
                        newPane.Width = newW;
                        newPane.Height = h;
                        // Focused pane becomes the left half.
                        context.CurrentFocusedPane.Width = newW;
                        break;
                    case "up":
                        newH = h / 2;
                        // New pane occupies the top half.
                        newPane.X = x;
                        newPane.Y = y;
                        newPane.Width = w;
                        newPane.Height = newH;
                        // Focused pane becomes the bottom half.
                        context.CurrentFocusedPane.Y = y + newH;
                        context.CurrentFocusedPane.Height = newH;
                        break;
                    case "down":
                        newH = h / 2;
                        // New pane occupies the bottom half.
                        newPane.X = x;
                        newPane.Y = y + newH;
                        newPane.Width = w;
                        newPane.Height = newH;
                        // Focused pane becomes the top half.
                        context.CurrentFocusedPane.Height = newH;
                        break;
                }
                // Add the new pane and update focus.
                context.CurrentTab.Panes.Add(newPane);
                context.CurrentFocusedPane = newPane;
            }
        }

        /// <summary>
        /// Handles the "focuspane" action by focusing on a specific pane within the current tab.
        /// </summary>
        /// <param name="action">The TabLayoutAction containing the action details, including the target pane index.</param>
        /// <param name="profileIcons">A dictionary mapping profile names to their corresponding icons (not used in this method).</param>
        /// <param name="context">The current tab context containing the current tab and focused pane.</param>
        /// <remarks>
        /// This method uses the "id" property from the action to determine the index of the pane to focus on within the current tab.
        /// If the index is valid, the specified pane becomes the current focused pane.
        /// </remarks>
        private static void HandleFocusPane(
            TabLayoutAction action,
            Dictionary<string, string> profileIcons,
            TabContext context)
        {
            // FocusPane uses property "id" (an index into the current tab’s panes).
            if (action.Id.HasValue && context.CurrentTab != null)
            {
                int target = action.Id.Value;
                if (target >= 0 && target < context.CurrentTab.Panes.Count)
                {
                    context.CurrentFocusedPane = context.CurrentTab.Panes[target];
                }
            }
        }

        /// <summary>
        /// Handles the "movefocus" action by updating the focused pane of the current tab based on the action's direction.
        /// </summary>
        /// <param name="action">The TabLayoutAction containing the action details, including the target direction.</param>
        /// <param name="profileIcons">A dictionary mapping profile names to their corresponding icons (not used in this method).</param>
        /// <param name="context">The current tab context containing the current tab and focused pane.</param>
        /// <remarks>
        /// If the action's direction is "previousinorder" or "nextinorder", this method will update the focused pane to the previous or next pane in the tab, respectively, wrapping around if at the beginning or end of the list of panes.
        /// </remarks>
        private static void HandleMoveFocus(
            TabLayoutAction action,
            Dictionary<string, string> profileIcons,
            TabContext context)
        {
            if (context.CurrentTab != null && context.CurrentTab.Panes.Count > 0 && context.CurrentFocusedPane != null)
            {
                int currentIndex = context.CurrentTab.Panes.IndexOf(context.CurrentFocusedPane);
                if (!string.IsNullOrWhiteSpace(action.Direction))
                {
                    string dir = action.Direction.ToLowerInvariant();
                    if (dir == "previousinorder")
                    {
                        int newIndex = (currentIndex - 1 + context.CurrentTab.Panes.Count) % context.CurrentTab.Panes.Count;
                        context.CurrentFocusedPane = context.CurrentTab.Panes[newIndex];
                    }
                    else if (dir == "nextinorder")
                    {
                        int newIndex = (currentIndex + 1) % context.CurrentTab.Panes.Count;
                        context.CurrentFocusedPane = context.CurrentTab.Panes[newIndex];
                    }
                }
            }
        }

        /// <summary>
        /// Handles the "switchToTab" action by switching the current tab to the one specified by the action's index property.
        /// </summary>
        /// <param name="action">The TabLayoutAction containing the action details, including the index of the tab to switch to.</param>
        /// <param name="profileIcons">A dictionary mapping profile names to their corresponding icons (not used in this method).</param>
        /// <param name="context">The current tab context containing the current tab and focused pane.</param>
        /// <remarks>
        /// If the action's index property is set to a valid index, this method will switch the current tab to the one at that index.
        /// If the newly switched tab has panes, the first pane in the list will be set as the focused pane.
        /// </remarks>
        private static void HandleSwitchTab(
            TabLayoutAction action,
            Dictionary<string, string> profileIcons,
            TabContext context)
        {
            // switchToTab uses property "index".
            if (action.Index.HasValue)
            {
                int tabIndex = action.Index.Value;
                if (tabIndex >= 0 && tabIndex < context.Tabs.Count)
                {
                    context.CurrentTab = context.Tabs[tabIndex];
                    if (context.CurrentTab.Panes.Count > 0)
                        context.CurrentFocusedPane = context.CurrentTab.Panes[0];
                }
            }
        }

        /// <summary>
        /// Parses the state.json file at the specified file path and returns a tooltip view model containing the state of the persisted window layout.
        /// </summary>
        /// <param name="filePath">The file path to the state.json file.</param>
        /// <param name="profileIcons">A dictionary mapping profile names to their corresponding icons.</param>
        /// <returns>A tooltip view model containing the state of the persisted window layout, or <c>null</c> if the file does not exist or is not a valid state.json file.</returns>
        /// <remarks>
        /// This method will return <c>null</c> if the file does not exist or is not a valid state.json file. Otherwise, it will create a tooltip view model containing the state of the persisted window layout by parsing the state.json file and executing the specified actions.
        /// </remarks>
        public static StateJsonTooltipViewModel? ParseState(string filePath, Dictionary<string, string> profileIcons)
        {
            var fileName = Path.GetFileName(filePath);
            if (!fileName.EndsWith("state.json"))
                return null;

            if (!File.Exists(filePath))
                return null;

            string json;
            try
            {
                json = File.ReadAllText(filePath);
            }
            catch (Exception)
            {
                return null;
            }

            StateJson? state;
            try
            {
                state = JsonSerializer.Deserialize<StateJson>(json, TerminalJsonOptions.SerializerOptions);
                if (state == null)
                    return null;
            }
            catch (Exception)
            {
                return null;
            }
            if (state.PersistedWindowLayouts == null || state.PersistedWindowLayouts.Count == 0)
                return null;

            var layout = state.PersistedWindowLayouts[0];
            if (layout.TabLayout == null || layout.TabLayout.Count == 0)
                return null;

            var tooltipVm = new StateJsonTooltipViewModel();
            // We also keep a pointer to the currently focused pane.
            TabContext context = new TabContext();

            foreach (var action in layout.TabLayout)
            {
                if (action.Action != null && actionHandlers.TryGetValue(action.Action, out var handler))
                {
                    handler(action, profileIcons, context);
                }
            }

            // For each tab, compute grid layout from pane geometries.
            foreach (var tab in context.Tabs)
            {
                ComputeGridLayout(tab);
                tooltipVm.TabStates.Add(tab);
            }
            return tooltipVm;
        }

        /// <summary>
        /// Compute grid placement for each pane using the geometry rectangles.
        /// </summary>
        /// <param name="tab">The tab state view model containing the panes.</param>
        /// <remarks>
        /// This algorithm:
        /// 1. Collects all unique X coordinates (pane.X and pane.X+pane.Width)
        ///    and all unique Y coordinates (pane.Y and pane.Y+pane.Height).
        /// 2. Sorts them and uses them as boundaries for grid columns and rows.
        /// 3. For each pane, sets GridColumn and GridColumnSpan and similarly for rows.
        /// </remarks>
        private static void ComputeGridLayout(TabStateViewModel tab)
        {
            var xCoords = new SortedSet<double>();
            var yCoords = new SortedSet<double>();

            foreach (var pane in tab.Panes)
            {
                xCoords.Add(pane.X);
                xCoords.Add(pane.X + pane.Width);
                yCoords.Add(pane.Y);
                yCoords.Add(pane.Y + pane.Height);
            }

            // Convert to list for index lookup.
            var xList = xCoords.ToList();
            var yList = yCoords.ToList();

            // The number of grid columns/rows.
            tab.GridColumns = xList.Count - 1;
            tab.GridRows = yList.Count - 1;

            foreach (var pane in tab.Panes)
            {
                // Find the start column index.
                int colStart = xList.FindIndex(x => Math.Abs(x - pane.X) < tolerance);
                // Find the end column index.
                int colEnd = xList.FindIndex(x => Math.Abs(x - (pane.X + pane.Width)) < tolerance);
                if (colStart == -1 || colEnd == -1)
                {
                    colStart = 0;
                    colEnd = 1;
                }
                pane.GridColumn = colStart;
                pane.GridColumnSpan = colEnd - colStart;

                int rowStart = yList.FindIndex(y => Math.Abs(y - pane.Y) < tolerance);
                int rowEnd = yList.FindIndex(y => Math.Abs(y - (pane.Y + pane.Height)) < tolerance);
                if (rowStart == -1 || rowEnd == -1)
                {
                    rowStart = 0;
                    rowEnd = 1;
                }
                pane.GridRow = rowStart;
                pane.GridRowSpan = rowEnd - rowStart;
            }
        }

        /// <summary>
        /// Maps a profile name to an icon path.
        /// </summary>
        /// <param name="profileName">The profile name to look up.</param>
        /// <param name="profileIcons">A dictionary mapping profile names to icon paths.</param>
        /// <returns>The icon path associated with the profile name if found, otherwise returns the default icon path.</returns>
        private static string GetIconForProfile(string? profileName, Dictionary<string, string> profileIcons)
        {
            // In a real app, you might look up profile details from settings.json.
            if (profileName != null && profileIcons.ContainsKey(profileName))
            {
                return profileIcons[profileName];
            }
            return "pack://application:,,,/WTLayoutManager;component/Assets/cmd.png";
        }
    }
}
