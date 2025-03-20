using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WTLayoutManager.Models
{
    public class ProfileInfo
    {
        public string? IconPath { get; set; }
        public string? ProfileName { get; set; }
    }

    public class SettingsJson
    {
        [JsonPropertyName("profiles")]
        public ProfilesSection Profiles { get; set; }
    }

    public class ProfilesSection
    {
        [JsonPropertyName("list")]
        public List<Profile> List { get; set; }
    }

    public class Profile
    {
        [JsonPropertyName("guid")]
        public string? GUID { get; set; }

        [JsonPropertyName("hidden")]
        public bool? Hidden { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("commandline")]
        public string? CommandLine { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }
    }
}
