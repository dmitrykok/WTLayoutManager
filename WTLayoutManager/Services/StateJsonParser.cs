using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WTLayoutManager.Models;
using WTLayoutManager.ViewModels;

namespace WTLayoutManager.Services
{
    public static class StateJsonParser
    {
        public static StateJsonTooltipViewModel ParseState(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            if (!fileName.EndsWith("state.json"))
                return null;

            if (!File.Exists(filePath))
                return null;

            string json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            StateJson state;
            try
            {
                state = JsonSerializer.Deserialize<StateJson>(json, options);
            }
            catch (Exception)
            {
                return null;
            }
            if (state?.PersistedWindowLayouts == null || state.PersistedWindowLayouts.Count == 0)
                return null;

            var layout = state.PersistedWindowLayouts[0];
            if (layout.TabLayout == null || layout.TabLayout.Count == 0)
                return null;

            var tooltipVm = new StateJsonTooltipViewModel();
            var tabs = new List<TabStateViewModel>();
            TabStateViewModel currentTab = null;
            // We also keep a pointer to the currently focused pane.
            PaneViewModel currentFocusedPane = null;

            foreach (var action in layout.TabLayout)
            {
                if (action.Action.Equals("newTab", StringComparison.OrdinalIgnoreCase))
                {
                    // Create a new tab.
                    currentTab = new TabStateViewModel();
                    // First pane occupies full area.
                    var pane = new PaneViewModel
                    {
                        ProfileName = action.Profile,
                        Icon = GetIconForProfile(action.Profile),
                        X = 0,
                        Y = 0,
                        Width = 1,
                        Height = 1,
                        GridRowSpan = 1,
                        GridColumnSpan = 1
                    };
                    currentTab.Panes.Add(pane);
                    currentFocusedPane = pane;
                    tabs.Add(currentTab);
                }
                else if (action.Action.Equals("splitPane", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentTab != null && currentFocusedPane != null)
                    {
                        // Clone the geometry of the currently focused pane.
                        double x = currentFocusedPane.X;
                        double y = currentFocusedPane.Y;
                        double w = currentFocusedPane.Width;
                        double h = currentFocusedPane.Height;
                        double newW, newH;
                        var splitDir = action.Split?.ToLowerInvariant();
                        PaneViewModel newPane = new PaneViewModel
                        {
                            ProfileName = action.Profile,
                            Icon = GetIconForProfile(action.Profile),
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
                                currentFocusedPane.X = x + newW;
                                currentFocusedPane.Width = newW;
                                break;
                            case "right":
                                newW = w / 2;
                                // New pane occupies right half.
                                newPane.X = x + newW;
                                newPane.Y = y;
                                newPane.Width = newW;
                                newPane.Height = h;
                                // Focused pane becomes the left half.
                                currentFocusedPane.Width = newW;
                                break;
                            case "up":
                                newH = h / 2;
                                // New pane occupies the top half.
                                newPane.X = x;
                                newPane.Y = y;
                                newPane.Width = w;
                                newPane.Height = newH;
                                // Focused pane becomes the bottom half.
                                currentFocusedPane.Y = y + newH;
                                currentFocusedPane.Height = newH;
                                break;
                            case "down":
                                newH = h / 2;
                                // New pane occupies the bottom half.
                                newPane.X = x;
                                newPane.Y = y + newH;
                                newPane.Width = w;
                                newPane.Height = newH;
                                // Focused pane becomes the top half.
                                currentFocusedPane.Height = newH;
                                break;
                            default:
                                // If no valid split direction, do nothing.
                                continue;
                        }
                        // Add the new pane and update focus.
                        currentTab.Panes.Add(newPane);
                        currentFocusedPane = newPane;
                    }
                }
                else if (action.Action.Equals("focusPane", StringComparison.OrdinalIgnoreCase))
                {
                    // FocusPane uses property "id" (an index into the current tab’s panes).
                    if (action.Id.HasValue && currentTab != null)
                    {
                        int target = action.Id.Value;
                        if (target >= 0 && target < currentTab.Panes.Count)
                        {
                            currentFocusedPane = currentTab.Panes[target];
                        }
                    }
                }
                else if (action.Action.Equals("switchToTab", StringComparison.OrdinalIgnoreCase))
                {
                    // switchToTab uses property "index".
                    if (action.Index.HasValue)
                    {
                        int tabIndex = action.Index.Value;
                        if (tabIndex >= 0 && tabIndex < tabs.Count)
                        {
                            currentTab = tabs[tabIndex];
                            if (currentTab.Panes.Count > 0)
                                currentFocusedPane = currentTab.Panes[0];
                        }
                    }
                }
                // (Other actions can be handled similarly if needed.)
            }

            // For each tab, compute grid layout from pane geometries.
            foreach (var tab in tabs)
            {
                ComputeGridLayout(tab);
                tooltipVm.TabStates.Add(tab);
            }
            return tooltipVm;
        }

        /// <summary>
        /// Compute grid placement for each pane using the geometry rectangles.
        /// This algorithm:
        /// 1. Collects all unique X coordinates (pane.X and pane.X+pane.Width)
        ///    and all unique Y coordinates (pane.Y and pane.Y+pane.Height).
        /// 2. Sorts them and uses them as boundaries for grid columns and rows.
        /// 3. For each pane, sets GridColumn and GridColumnSpan and similarly for rows.
        /// </summary>
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
                int colStart = xList.FindIndex(x => Math.Abs(x - pane.X) < 0.0001);
                // Find the end column index.
                int colEnd = xList.FindIndex(x => Math.Abs(x - (pane.X + pane.Width)) < 0.0001);
                if (colStart == -1 || colEnd == -1)
                {
                    colStart = 0;
                    colEnd = 1;
                }
                pane.GridColumn = colStart;
                pane.GridColumnSpan = colEnd - colStart;

                int rowStart = yList.FindIndex(y => Math.Abs(y - pane.Y) < 0.0001);
                int rowEnd = yList.FindIndex(y => Math.Abs(y - (pane.Y + pane.Height)) < 0.0001);
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
        /// A simple mapping from profile name to an icon path.
        /// </summary>
        private static string GetIconForProfile(string profileName)
        {
            // In a real app, you might look up profile details from settings.json.
            return "pack://application:,,,/WTLayoutManager;component/Assets/cmd.png";
        }
    }
}
