using System.Diagnostics.CodeAnalysis;

namespace Windows.ApplicationModel;
public static class PackageVersionExtensions
{
    /// <summary>
    /// Parses a version string into a PackageVersion object.
    /// </summary>
    /// <param name="version">The version string to parse.</param>
    /// <returns>A new PackageVersion object representing the parsed version.</returns>
    public static PackageVersion Parse(this PackageVersion current, string version)
    {
        Version _version = Version.Parse(version);
        return new PackageVersion
        {
            Major = (ushort)_version.Major,
            Minor = (ushort)_version.Minor,
            Build = (ushort)_version.Build,
            Revision = (ushort)_version.Revision
        };
    }

    /// <summary>
    /// Attempts to parse a string representation of a version into a PackageVersion instance.
    /// </summary>
    /// <param name="current">The current PackageVersion instance (this method is an extension method).</param>
    /// <param name="version">The string representation of the version to parse.</param>
    /// <param name="packageVersion">The parsed PackageVersion instance if successful, otherwise null.</param>
    /// <returns>True if the version string was successfully parsed, otherwise false.</returns>
    public static bool TryParse(this PackageVersion current, string version, [NotNullWhen(true)] out PackageVersion? packageVersion)
    {
        packageVersion = null;
        if (Version.TryParse(version, out Version? parsedVersion))
        {
            packageVersion = new PackageVersion
            {
                Major = (ushort)parsedVersion.Major,
                Minor = (ushort)parsedVersion.Minor,
                Build = (ushort)parsedVersion.Build,
                Revision = (ushort)parsedVersion.Revision
            };
            return true;
        }
        return false;
    }

    /// <summary>
    /// Compares the current PackageVersion instance with another PackageVersion instance.
    /// </summary>
    /// <param name="current">The current PackageVersion instance (this method is an extension method).</param>
    /// <param name="other">The PackageVersion instance to compare with.</param>
    /// <returns>A negative integer, zero, or a positive integer if the current version is less than, equal to, or greater than the other version, respectively.</returns>
    public static int CompareTo(this PackageVersion current, PackageVersion other)
    {
        int result = current.Major.CompareTo(other.Major);
        if (result != 0) return result;
        result = current.Minor.CompareTo(other.Minor);
        if (result != 0) return result;
        result = current.Build.CompareTo(other.Build);
        if (result != 0) return result;
        return current.Revision.CompareTo(other.Revision);
    }

    /// <summary>
    /// Checks if the current PackageVersion instance is at least the required version.
    /// </summary>
    /// <param name="current">The current PackageVersion instance (this method is an extension method).</param>
    /// <param name="requiredVersion">The required version string to compare with.</param>
    /// <returns>True if the current version is at least the required version, otherwise false.</returns>
    public static bool IsAtLeast(this PackageVersion current, string requiredVersion)
    {
        var req = current.Parse(requiredVersion);
        return current.CompareTo(req) >= 0;
    }

    // Additional extension methods simulating operators:

    /// <summary>
    /// Checks if the current PackageVersion instance is greater than the specified version.
    /// </summary>
    /// <param name="current">The current PackageVersion instance (this method is an extension method).</param>
    /// <param name="other">The PackageVersion instance to compare with.</param>
    /// <returns>True if the current version is greater than the specified version, otherwise false.</returns>
    // ">"
    public static bool IsGreaterThan(this PackageVersion current, PackageVersion other)
    {
        // Compare the current version with the other version and return true if it's greater
        return current.CompareTo(other) > 0;
    }

    /// <summary>
    /// Checks if the current PackageVersion instance is less than the specified version.
    /// </summary>
    /// <param name="current">The current PackageVersion instance (this method is an extension method).</param>
    /// <param name="other">The PackageVersion instance to compare with.</param>
    /// <returns>True if the current version is less than the specified version, otherwise false.</returns>
    // "<"
    public static bool IsLessThan(this PackageVersion current, PackageVersion other)
    {
        // Compare the current version with the other version and return true if it's less
        return current.CompareTo(other) < 0;
    }

    /// <summary>
    /// Checks if the current PackageVersion instance is less than or equal to the specified version.
    /// </summary>
    /// <param name="current">The current PackageVersion instance (this method is an extension method).</param>
    /// <param name="other">The PackageVersion instance to compare with.</param>
    /// <returns>True if the current version is less than or equal to the specified version, otherwise false.</returns>
    // "<="
    public static bool IsLessThanOrEqualTo(this PackageVersion current, PackageVersion other)
    {
        // Compare the current version with the other version and return true if it's less than or equal
        return current.CompareTo(other) <= 0;
    }

    /// <summary>
    /// Checks if the current PackageVersion instance is greater than or equal to the specified version.
    /// </summary>
    /// <param name="current">The current PackageVersion instance (this method is an extension method).</param>
    /// <param name="other">The PackageVersion instance to compare with.</param>
    /// <returns>True if the current version is greater than or equal to the specified version, otherwise false.</returns>
    // ">="
    public static bool IsGreaterThanOrEqualTo(this PackageVersion current, PackageVersion other)
    {
        // Compare the current version with the other version and return true if it's greater than or equal to
        return current.CompareTo(other) >= 0;
    }

    /// <summary>
    /// Checks if the current PackageVersion instance is greater than the specified version.
    /// </summary>
    /// <param name="current">The current PackageVersion instance (this method is an extension method).</param>
    /// <param name="otherVersion">The version string to compare with.</param>
    /// <returns>True if the current version is greater than the specified version, otherwise false.</returns>
    public static bool IsGreaterThan(this PackageVersion current, string otherVersion)
    {
        var other = current.Parse(otherVersion);
        return current.IsGreaterThan(other);
    }

    /// <summary>
    /// Checks if the current PackageVersion instance is less than the specified version.
    /// </summary>
    /// <param name="current">The current PackageVersion instance (this method is an extension method).</param>
    /// <param name="otherVersion">The version string to compare with.</param>
    /// <returns>True if the current version is less than the specified version, otherwise false.</returns>
    public static bool IsLessThan(this PackageVersion current, string otherVersion)
    {
        var other = current.Parse(otherVersion);
        return current.IsLessThan(other);
    }

    /// <summary>
    /// Checks if the current PackageVersion instance is less than or equal to the specified version string.
    /// </summary>
    /// <param name="current">The current PackageVersion instance (this method is an extension method).</param>
    /// <param name="otherVersion">The version string to compare with.</param>
    /// <returns>True if the current version is less than or equal to the specified version, otherwise false.</returns>
    public static bool IsLessThanOrEqualTo(this PackageVersion current, string otherVersion)
    {
        var other = current.Parse(otherVersion);
        return current.IsLessThanOrEqualTo(other);
    }

    /// <summary>
    /// Checks if the current PackageVersion instance is greater than or equal to the specified version.
    /// </summary>
    /// <param name="current">The current PackageVersion instance (this method is an extension method).</param>
    /// <param name="otherVersion">The version string to compare with.</param>
    /// <returns>True if the current version is greater than or equal to the specified version, otherwise false.</returns>
    public static bool IsGreaterThanOrEqualTo(this PackageVersion current, string otherVersion)
    {
        var other = current.Parse(otherVersion);
        return current.IsGreaterThanOrEqualTo(other);
    }
}
