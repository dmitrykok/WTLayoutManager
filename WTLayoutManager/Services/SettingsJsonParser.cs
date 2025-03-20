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
            catch (IOException)
            {
                // If an exception is thrown, the resource doesn't exist
                return false;
            }
        }

        private static string ResolveIconPath(Profile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.Icon))
            {
                var guidImg = $"pack://application:,,,/WTLayoutManager;component/Assets/{Path.GetFileName(profile.GUID)}.png";
                if (ResourceExists(guidImg))
                {
                    return guidImg;
                }
                if (profile?.Source?.EndsWith(".Wsl") == true)
                {
                    return "pack://application:,,,/WTLayoutManager;component/Assets/wsl.png";
                }
                if (profile?.Source?.EndsWith("Git") == true)
                {
                    return "pack://application:,,,/WTLayoutManager;component/Assets/git-bash.png";
                }
                if (profile?.Source?.EndsWith(".VisualStudio") == true)
                {
                    if (profile?.Name?.Contains("Windows PowerShell") == true)
                    {
                        return "pack://application:,,,/WTLayoutManager;component/Assets/vs-powershell.png";
                    }
                    if (profile?.Name?.Contains("PowerShell") == true)
                    {
                        return "pack://application:,,,/WTLayoutManager;component/Assets/vs-pwsh.png";
                    }
                    if (profile?.Name?.Contains("Command Prompt") == true)
                    {
                        return "pack://application:,,,/WTLayoutManager;component/Assets/vs-cmd.png";
                    }
                }
                if (profile?.Name?.Contains("Windows PowerShell") == true)
                {
                    return "pack://application:,,,/WTLayoutManager;component/Assets/powershell.png";
                }
                if (profile?.Name?.Contains("PowerShell") == true && profile?.Name?.Contains("Preview") == true)
                {
                    return "pack://application:,,,/WTLayoutManager;component/Assets/pwsh-preview.png";
                }
                if (profile?.Name?.Contains("PowerShell") == true)
                {
                    return "pack://application:,,,/WTLayoutManager;component/Assets/pwsh.png";
                }
                return "pack://application:,,,/WTLayoutManager;component/Assets/cmd.png"; // Default fallback
            }

            if (profile.Icon.StartsWith("ms-appx:///"))
            {
                // Map ms-appx URI to local resources or handle accordingly
                return $"pack://application:,,,/WTLayoutManager;component/Assets/{Path.GetFileName(profile.Icon)}";
            }

            var expandedIcon = Environment.ExpandEnvironmentVariables(profile.Icon);
            if (Path.Exists(expandedIcon))
            {
                return expandedIcon;
            }

            if (profile?.Name?.Contains("Windows PowerShell") == true)
            {
                return "pack://application:,,,/WTLayoutManager;component/Assets/powershell.png";
            }
            if (profile?.Name?.Contains("PowerShell") == true && profile?.Name?.Contains("Preview") == true)
            {
                return "pack://application:,,,/WTLayoutManager;component/Assets/pwsh-preview.png";
            }
            if (profile?.Name?.Contains("PowerShell") == true)
            {
                return "pack://application:,,,/WTLayoutManager;component/Assets/pwsh.png";
            }

            return "pack://application:,,,/WTLayoutManager;component/Assets/cmd.png"; // Default fallback
        }
    }
}
