using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Resources;
using WTLayoutManager.Models;
using WTLayoutManager.ViewModels;
using System;
using System.IO;
using System.Windows;
using System.Windows.Resources;

namespace WTLayoutManager.Services
{
    public static class SettingsJsonParser
    {
        public static IEnumerable<ProfileInfo> GetProfileInfos(string filePath)
        {
            if (!File.Exists(filePath))
                yield break;

            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            var settings = JsonSerializer.Deserialize<SettingsJson>(json, options);

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

        // Define your mapping rules (order matters)
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
