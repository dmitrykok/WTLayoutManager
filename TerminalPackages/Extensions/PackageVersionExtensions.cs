using System.Diagnostics.CodeAnalysis;

namespace Windows.ApplicationModel;
public static class PackageVersionExtensions
{
    // Parse a version string in the form "Major.Minor.Build.Revision"
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

    // Compare two PackageVersion values
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

    // Check if current version is greater than or equal to a required version specified as a string
    public static bool IsAtLeast(this PackageVersion current, string requiredVersion)
    {
        var req = current.Parse(requiredVersion);
        return current.CompareTo(req) >= 0;
    }

    // Additional extension methods simulating operators:

    // ">"
    public static bool IsGreaterThan(this PackageVersion current, PackageVersion other)
    {
        return current.CompareTo(other) > 0;
    }

    // "<"
    public static bool IsLessThan(this PackageVersion current, PackageVersion other)
    {
        return current.CompareTo(other) < 0;
    }

    // "<="
    public static bool IsLessThanOrEqualTo(this PackageVersion current, PackageVersion other)
    {
        return current.CompareTo(other) <= 0;
    }

    // ">="
    public static bool IsGreaterThanOrEqualTo(this PackageVersion current, PackageVersion other)
    {
        return current.CompareTo(other) >= 0;
    }

    // Optionally, overload with string parameter for convenience:
    public static bool IsGreaterThan(this PackageVersion current, string otherVersion)
    {
        var other = current.Parse(otherVersion);
        return current.IsGreaterThan(other);
    }

    public static bool IsLessThan(this PackageVersion current, string otherVersion)
    {
        var other = current.Parse(otherVersion);
        return current.IsLessThan(other);
    }

    public static bool IsLessThanOrEqualTo(this PackageVersion current, string otherVersion)
    {
        var other = current.Parse(otherVersion);
        return current.IsLessThanOrEqualTo(other);
    }

    public static bool IsGreaterThanOrEqualTo(this PackageVersion current, string otherVersion)
    {
        var other = current.Parse(otherVersion);
        return current.IsGreaterThanOrEqualTo(other);
    }
}
