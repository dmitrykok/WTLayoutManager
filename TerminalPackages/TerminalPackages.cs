// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.Management.Deployment;

namespace WTLayoutManager.Services
{
    /// <summary>
    /// Represents detailed information about an installed Windows Terminal package.
    /// </summary>
    /// <remarks>
    /// Contains properties capturing package metadata such as name, publisher, version, and installation details.
    /// Supports creating a deep copy of terminal package information.
    /// </remarks>
    /// <seealso cref="PackageVersionEx"/>
    /// <seealso cref="TerminalPackages"/>
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
        string installedLocationPath
    )
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

        /// <summary>
        /// Creates a deep copy of the current <see cref="TerminalInfo"/> instance.
        /// </summary>
        /// <returns>A new <see cref="TerminalInfo"/> instance with the same properties as the current instance.</returns>
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

    /// <summary>
    /// Provides utility methods for discovering and managing installed Windows Terminal packages.
    /// </summary>
    /// <remarks>
    /// Includes methods for extracting common names from distinguished names and finding installed terminal packages.
    /// </remarks>
    /// <seealso cref="PackageManager"/>
    /// <seealso cref="TerminalInfo"/>
    public class TerminalPackages
    {
        /// <summary>
        /// A regular expression pattern to extract the Common Name (CN) from a distinguished name string.
        /// </summary>
        /// <remarks>
        /// Matches CN values with or without quotes, ignoring case and allowing flexible whitespace.
        /// </remarks>
        private static readonly Regex _regex = new Regex(@"CN\s*=\s*(?<cn>(""[^""]+"")|[^,]+)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Provides default JSON serialization options for terminal package-related operations.
        /// </summary>
        /// <remarks>
        /// Configures JSON serialization with custom package version conversion, indented formatting,
        /// comment skipping, and trailing comma support.
        /// </remarks>
        public static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions
        {
            Converters =
            {
                new PackageVersionConverter()
            },
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>
        /// Extracts the Common Name (CN) from a distinguished name string.
        /// </summary>
        /// <param name="distinguishedName">The distinguished name string.</param>
        /// <returns>The extracted Common Name (CN) value.</returns>
        /// <exception cref="ArgumentException">Thrown if the distinguished name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the CN value is not found in the distinguished name.</exception>
        static string GetCommonName(string distinguishedName)
        {
            if (string.IsNullOrWhiteSpace(distinguishedName))
            {
                throw new ArgumentException("Distinguished name is null or empty.", nameof(distinguishedName));
            }

            // Use regex to extract the CN value
            var match = _regex.Match(distinguishedName);

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

        /// <summary>
        /// Finds all installed terminal packages on the system, extracting the display name,
        /// common name, full name, family name, name, publisher, publisher ID, version, installed
        /// date, publisher display name, logo absolute URI, and installed location path.
        /// </summary>
        /// <returns>
        /// A dictionary of terminal information keyed by a formatted string containing the display
        /// name, common name, and the string "  \t-> \"". The value associated with each key
        /// is an instance of <see cref="TerminalInfo"/>.
        /// </returns>
        /// <remarks>
        /// Includes only packages with a family name containing the string "Terminal".
        /// </remarks>
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