using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WTLayoutManager.Models
{
    /// <summary>
    /// Represents additional information about a profile, including its icon path and name.
    /// </summary>
    public class ProfileInfo
    {
        public string? IconPath { get; set; }
        public string? ProfileName { get; set; }
    }

    /// <summary>
    /// Represents the JSON structure for Windows Terminal settings, containing profile configurations.
    /// </summary>
    public class SettingsJson
    {
        [JsonPropertyName("profiles")]
        public ProfilesSection? Profiles { get; set; }
    }

    /// <summary>
    /// Represents a section of profiles in the Windows Terminal settings, containing a list of terminal profiles.
    /// </summary>
    public class ProfilesSection
    {
        [JsonPropertyName("list")]
        public List<Profile>? List { get; set; }
    }

    /// <summary>
    /// Represents a Windows Terminal profile configuration with its associated properties.
    /// </summary>
    /// <remarks>
    /// Contains details such as the profile's unique identifier, visibility, source, command line, name, and icon.
    /// </remarks>
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
