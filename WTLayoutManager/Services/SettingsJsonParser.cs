using System.IO;
using System.Text.Json;
using System.Windows.Resources;
using WTLayoutManager.Models;
using System.Windows;

/// <summary>
/// Provides utility methods for parsing and processing Windows Terminal settings and profile configurations.
/// </summary>
/// <remarks>
/// This static class contains methods for extracting profile information from settings.json,
/// resolving icon paths for terminal profiles, and checking resource existence.
/// </remarks>
namespace WTLayoutManager.Services
{
    public static class SettingsJsonParser
    {
        /// <summary>
        /// Returns an enumerable sequence of <see cref="ProfileInfo"/> instances by parsing the specified file path to a settings.json file.
        /// </summary>
        /// <remarks>
        /// This method will only return a non-empty enumeration if the specified file path is a valid settings.json file.
        /// </remarks>
        /// <param name="filePath">The file path to the settings.json file.</param>
        /// <returns>An enumerable sequence of <see cref="ProfileInfo"/> instances.</returns>
        public static IEnumerable<ProfileInfo> GetProfileInfos(string filePath)
        {
            if (Path.GetFileName(filePath) != "settings.json")
                yield break;

            if (!File.Exists(filePath))
                yield break;

            var json = File.ReadAllText(filePath);
            var settings = JsonSerializer.Deserialize<SettingsJson>(json, TerminalJsonOptions.SerializerOptions);

            if (settings?.Profiles?.List == null)
                yield break;

            foreach (var profile in settings.Profiles.List)
            {
                if (string.IsNullOrWhiteSpace(profile.Name))
                    continue;

                if (profile.Hidden == true)
                    continue;

                yield return new ProfileInfo
                {
                    ProfileName = profile.Name,
                    IconPath = ResolveIconPath(profile)
                };
            }
        }

        /// <summary>
        /// Determines whether the specified pack URI string refers to a resource that exists within the application's package.
        /// </summary>
        /// <param name="packUriString">The pack URI string to check.</param>
        /// <returns>true if the resource exists; otherwise, false.</returns>
        public static bool ResourceExists(string packUriString)
        {
            var uri = new Uri(packUriString, UriKind.Absolute);

            try
            {
                // Attempt to retrieve the resource stream
                StreamResourceInfo resourceInfo = Application.GetResourceStream(uri);
                // If resourceInfo is null, the resource doesn't exist
                return resourceInfo != null;
            }
            catch
            {
                // If an exception is thrown, the resource doesn't exist
                return false;
            }
        }

        /// <summary>
        /// Defines a list of icon mapping rules for different terminal profiles based on their source and name.
        /// Each rule consists of a predicate function to match a profile and a corresponding icon path.
        /// </summary>
        private static readonly List<(Func<Profile, bool> Check, string Icon)> IconMappings = new List<(Func<Profile, bool>, string)>
        {
            (p => p?.Source?.EndsWith(".Wsl") == true, "pack://application:,,,/WTLayoutManager;component/Assets/wsl.png"),
            (p => p?.Source?.EndsWith("Git") == true, "pack://application:,,,/WTLayoutManager;component/Assets/git-bash.png"),
            (p => p?.Source?.EndsWith(".VisualStudio") == true && p?.Name?.Contains("Windows PowerShell") == true, "pack://application:,,,/WTLayoutManager;component/Assets/vs-powershell.png"),
            (p => p?.Source?.EndsWith(".VisualStudio") == true && p?.Name?.Contains("PowerShell") == true, "pack://application:,,,/WTLayoutManager;component/Assets/vs-pwsh.png"),
            (p => p?.Source?.EndsWith(".VisualStudio") == true && p?.Name?.Contains("Command Prompt") == true, "pack://application:,,,/WTLayoutManager;component/Assets/vs-cmd.png"),
            (p => p?.Name?.Contains("Windows PowerShell") == true, "pack://application:,,,/WTLayoutManager;component/Assets/powershell.png"),
            (p => p?.Name?.Contains("PowerShell") == true && p?.Name?.Contains("Preview") == true, "pack://application:,,,/WTLayoutManager;component/Assets/pwsh-preview.png"),
            (p => p?.Name?.Contains("PowerShell") == true, "pack://application:,,,/WTLayoutManager;component/Assets/pwsh.png")
        };

        /// <summary>
        /// Resolves the icon path for a given terminal profile.
        /// If the profile's icon is empty, it will be resolved based on the profile's name and source.
        /// If the profile's icon is not empty and starts with "ms-appx:///", it will be expanded to a valid resource path.
        /// If the profile's icon is not empty and does not start with "ms-appx:///", it will be expanded to a valid file path using environment variables.
        /// If the expanded icon path does not exist, it will be resolved based on the profile's name and source.
        /// If the expanded icon path exists, it will be returned.
        /// If the profile's icon is empty and the expanded icon path does not exist, the default fallback icon will be returned.
        /// </summary>
        /// <param name="profile">The profile to resolve the icon path for.</param>
        /// <returns>The resolved icon path for the given profile.</returns>
        private static string ResolveIconPath(Profile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.Icon))
            {
                var guidImg = $"pack://application:,,,/WTLayoutManager;component/Assets/{Path.GetFileName(profile.GUID)}.png";
                if (ResourceExists(guidImg))
                    return guidImg;

                // Iterate rules to select icon
                foreach (var rule in IconMappings)
                {
                    if (rule.Check(profile))
                        return rule.Icon;
                }

                // Default fallback
                return "pack://application:,,,/WTLayoutManager;component/Assets/cmd.png";
            }

            // Handle non-empty profile.Icon logic as before…
            if (profile.Icon.StartsWith("ms-appx:///"))
                return $"pack://application:,,,/WTLayoutManager;component/Assets/{Path.GetFileName(profile.Icon)}";

            var expandedIcon = Environment.ExpandEnvironmentVariables(profile.Icon);
            if (Path.Exists(expandedIcon))
                return expandedIcon;

            foreach (var rule in IconMappings)
            {
                if (rule.Check(profile))
                    return rule.Icon;
            }

            return "pack://application:,,,/WTLayoutManager;component/Assets/cmd.png";
        }
    }
}
