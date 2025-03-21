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
            // Maintain the current focused pane's coordinates within the current tab.
            int currentFocusRow = 0, currentFocusColumn = 0;

            foreach (var action in layout.TabLayout)
            {
                if (action.Action.Equals("newTab", StringComparison.OrdinalIgnoreCase))
                {
                    // Create a new tab; the first pane is placed at (0,0).
                    currentTab = new TabStateViewModel();
                    var pane = new PaneViewModel
                    {
                        ProfileName = action.Profile,
                        Icon = GetIconForProfile(action.Profile),
                        GridRow = 0,
                        GridColumn = 0,
                        GridRowSpan = 1,
                        GridColumnSpan = 1
                    };
                    currentTab.Panes.Add(pane);
                    currentFocusRow = 0;
                    currentFocusColumn = 0;
                    tabs.Add(currentTab);
                }
                else if (action.Action.Equals("splitPane", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentTab != null)
                    {
                        int newRow = currentFocusRow;
                        int newCol = currentFocusColumn;
                        // Determine the new pane's position based on the split direction.
                        string splitDir = action.Split?.ToLowerInvariant();
                        if (splitDir == "up")
                        {
                            newRow = currentFocusRow - 1;
                        }
                        else if (splitDir == "down")
                        {
                            newRow = currentFocusRow + 1;
                        }
                        else if (splitDir == "left")
                        {
                            newCol = currentFocusColumn - 1;
                        }
                        else if (splitDir == "right")
                        {
                            newCol = currentFocusColumn + 1;
                        }
                        // Create the new pane.
                        var pane = new PaneViewModel
                        {
                            ProfileName = action.Profile,
                            Icon = GetIconForProfile(action.Profile),
                            GridRow = newRow,
                            GridColumn = newCol,
                            GridRowSpan = 1,
                            GridColumnSpan = 1,
                            SplitDirection = splitDir
                        };
                        currentTab.Panes.Add(pane);
                        // Update focus to the new pane.
                        currentFocusRow = newRow;
                        currentFocusColumn = newCol;
                    }
                }
                else if (action.Action.Equals("focusPane", StringComparison.OrdinalIgnoreCase))
                {
                    // Use the 'id' property to update focus.
                    if (action.Id.HasValue && currentTab != null)
                    {
                        int target = action.Id.Value;
                        if (target >= 0 && target < currentTab.Panes.Count)
                        {
                            currentFocusRow = currentTab.Panes[target].GridRow;
                            currentFocusColumn = currentTab.Panes[target].GridColumn;
                        }
                    }
                }
                else if (action.Action.Equals("switchToTab", StringComparison.OrdinalIgnoreCase))
                {
                    // Use the 'index' property to switch tabs.
                    if (action.Index.HasValue)
                    {
                        int tabIndex = action.Index.Value;
                        if (tabIndex >= 0 && tabIndex < tabs.Count)
                        {
                            currentTab = tabs[tabIndex];
                            // Reset focus to the first pane of that tab.
                            if (currentTab.Panes.Count > 0)
                            {
                                currentFocusRow = currentTab.Panes[0].GridRow;
                                currentFocusColumn = currentTab.Panes[0].GridColumn;
                            }
                        }
                    }
                }
                // You can extend handling for additional actions as needed.
            }

            // Adjust coordinates so that the smallest row/column is 0 for each tab.
            foreach (var tab in tabs)
            {
                if (tab.Panes.Count == 0) continue;
                int minRow = int.MaxValue, minCol = int.MaxValue;
                int maxRow = int.MinValue, maxCol = int.MinValue;
                foreach (var pane in tab.Panes)
                {
                    if (pane.GridRow < minRow) minRow = pane.GridRow;
                    if (pane.GridColumn < minCol) minCol = pane.GridColumn;
                    if (pane.GridRow > maxRow) maxRow = pane.GridRow;
                    if (pane.GridColumn > maxCol) maxCol = pane.GridColumn;
                }
                foreach (var pane in tab.Panes)
                {
                    pane.GridRow -= minRow;
                    pane.GridColumn -= minCol;
                }
                tab.GridRows = maxRow - minRow + 1;
                tab.GridColumns = maxCol - minCol + 1;
            }

            // Add all tabs to the tooltip view model.
            foreach (var tab in tabs)
            {
                tooltipVm.TabStates.Add(tab);
            }
            return tooltipVm;
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
