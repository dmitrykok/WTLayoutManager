using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WTLayoutManager.Models
{
    public class StateJson
    {
        [JsonPropertyName("persistedWindowLayouts")]
        public List<PersistedWindowLayout>? PersistedWindowLayouts { get; set; }
    }

    public class PersistedWindowLayout
    {
        [JsonPropertyName("tabLayout")]
        public List<TabLayoutAction>? TabLayout { get; set; }
    }

    public class TabLayoutAction
    {
        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("commandline")]
        public string? Commandline { get; set; }

        [JsonPropertyName("profile")]
        public string? Profile { get; set; }

        [JsonPropertyName("sessionId")]
        public string? SessionId { get; set; }

        [JsonPropertyName("startingDirectory")]
        public string? StartingDirectory { get; set; }

        [JsonPropertyName("suppressApplicationTitle")]
        public bool SuppressApplicationTitle { get; set; }

        [JsonPropertyName("tabTitle")]
        public string? TabTitle { get; set; }

        [JsonPropertyName("size")]
        public double? Size { get; set; }

        [JsonPropertyName("split")]
        public string? Split { get; set; }

        // For focusPane actions.
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        // For switchToTab actions.
        [JsonPropertyName("index")]
        public int? Index { get; set; }

        // For moveFocus actions.
        [JsonPropertyName("direction")]
        public string? Direction { get; set; }
    }
}
