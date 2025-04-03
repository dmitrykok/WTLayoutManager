using System;
using Windows.ApplicationModel;
using WTLayoutManager.Services;

public static class PackageVersionExtensions
{
    // Parse a version string in the form "Major.Minor.Build.Revision"
    public static PackageVersion ParseVersion(this string version)
    {
        var parts = version.Split('.');
        if (parts.Length != 4)
        {
            throw new ArgumentException("Version string must be in the format Major.Minor.Build.Revision");
        }
        return new PackageVersion
        {
            Major = ushort.Parse(parts[0]),
            Minor = ushort.Parse(parts[1]),
            Build = ushort.Parse(parts[2]),
            Revision = ushort.Parse(parts[3])
        };
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
    public static bool IsAtLeast(this PackageVersion current, PackageVersion other)
    {
        return CompareTo(current, other) >= 0;
    }

    // Additional extension methods simulating operators:

    // ">"
    public static bool IsGreaterThan(this PackageVersion current, PackageVersion other)
    {
        return CompareTo(current, other) > 0;
    }

    // "<"
    public static bool IsLessThan(this PackageVersion current, PackageVersion other)
    {
        return CompareTo(current, other) < 0;
    }

    // "<="
    public static bool IsLessThanOrEqualTo(this PackageVersion current, PackageVersion other)
    {
        return CompareTo(current, other) <= 0;
    }

    // ">="
    public static bool IsGreaterThanOrEqualTo(this PackageVersion current, PackageVersion other)
    {
        return CompareTo(current, other) >= 0;
    }

    // Optionally, overload with string parameter for convenience:
    public static bool IsGreaterThan(this PackageVersion current, string otherVersion)
    {
        var other = ParseVersion(otherVersion);
        return IsGreaterThan(current, other);
    }

    public static bool IsLessThan(this PackageVersion current, string otherVersion)
    {
        var other = ParseVersion(otherVersion);
        return IsLessThan(current, other);
    }

    public static bool IsLessThanOrEqualTo(this PackageVersion current, string otherVersion)
    {
        var other = ParseVersion(otherVersion);
        return IsLessThanOrEqualTo(current, other);
    }

    public static bool IsGreaterThanOrEqualTo(this PackageVersion current, string otherVersion)
    {
        var other = ParseVersion(otherVersion);
        return IsGreaterThanOrEqualTo(current, other);
    }
}
