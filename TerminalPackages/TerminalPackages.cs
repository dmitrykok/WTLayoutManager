// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.Management.Deployment;

namespace WTLayoutManager.Services
{
    [Serializable]
    public class TerminalInfo(
    string fullName,
    string familyName,
    string name,
    string publisher,
    string publisherId,
    PackageVersionEx version,
    DateTimeOffset installedDate,
    string publisherDisplayName,
    string displayName,
    string logoAbsoluteUri,
    string installedLocationPath)
    {
        public string FullName { get; set; } = fullName;
        public string FamilyName { get; set; } = familyName;
        public string Name { get; set; } = name;
        public string Publisher { get; set; } = publisher;
        public string PublisherId { get; set; } = publisherId;
        public PackageVersionEx Version { get; set; } = version;
        public DateTimeOffset InstalledDate { get; set; } = installedDate;
        public string PublisherDisplayName { get; set; } = publisherDisplayName;
        public string DisplayName { get; set; } = displayName;
        public string LogoAbsoluteUri { get; set; } = logoAbsoluteUri;
        public string InstalledLocationPath { get; set; } = installedLocationPath;
        public List<string> LocalStateFiles { get; set; } = new List<string>();

        public TerminalInfo Clone()
        {
            return new TerminalInfo(
                FullName,
                FamilyName,
                Name,
                Publisher,
                PublisherId,
                Version,
                InstalledDate,
                PublisherDisplayName,
                DisplayName,
                LogoAbsoluteUri,
                InstalledLocationPath)
            {
                // Ensure a deep copy of the list by copying each element
                LocalStateFiles = new List<string>(LocalStateFiles)
            };
        }
    }

    public class TerminalPackages
    {
        static string GetCommonName(string distinguishedName)
        {
            if (string.IsNullOrWhiteSpace(distinguishedName))
            {
                throw new ArgumentException("Distinguished name is null or empty.", nameof(distinguishedName));
            }

            var match = Regex.Match(distinguishedName, @"CN\s*=\s*(?<cn>(""[^""]+"")|[^,]+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var cnValue = match.Groups["cn"].Value.Trim();

                // Remove quotes if present
                if (cnValue.StartsWith("\"") && cnValue.EndsWith("\""))
                {
                    cnValue = cnValue.Substring(1, cnValue.Length - 2);
                }

                return cnValue;
            }

            throw new InvalidOperationException("CN not found in the distinguished name.");
        }

        public static Dictionary<string, TerminalInfo> FindInstalledTerminals()
        {
            var terminals = new Dictionary<string, TerminalInfo>();
            var packageManager = new PackageManager();

            var packages = packageManager.FindPackages();

            foreach (var package in packages)
            {
                if (package.Id.FamilyName.Contains("Terminal"))
                {
                    string cn = GetCommonName(package.Id.Publisher);
                    terminals[String.Format("{0, -36}  \t-> \"{1}\"", package.DisplayName, cn)] = new TerminalInfo(
                        package.Id.FullName,
                        package.Id.FamilyName,
                        package.Id.Name,
                        package.Id.Publisher,
                        package.Id.PublisherId,
                        package.Id.Version,
                        package.InstalledDate,
                        package.PublisherDisplayName,
                        package.DisplayName,
                        package.Logo.AbsoluteUri,
                        package.InstalledLocation.Path);
                }
            }

            return terminals;
        }
    }
}