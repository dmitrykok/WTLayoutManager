using System;
using Windows.ApplicationModel;

namespace WTLayoutManager.Services
{
    public struct PackageVersionEx : IComparable<PackageVersionEx>
    {
        public PackageVersion Version { get; }

        public PackageVersionEx(PackageVersion version)
        {
            Version = version;
        }

        public PackageVersionEx(PackageVersionEx version)
        {
            Version = version.Version;
        }

        public PackageVersionEx(string version)
        {
            Version = new PackageVersion().Parse(version);
        }

        public PackageVersionEx(ushort _Major, ushort _Minor, ushort _Build, ushort _Revision)
        {
            Version = new PackageVersion(_Major, _Minor, _Build, _Revision);
        }

        // Parse a version string in the format "Major.Minor.Build.Revision"
        public static PackageVersionEx Parse(string version)
        {
            return new PackageVersionEx(version);
        }

        public int CompareTo(PackageVersionEx other)
        {
            int result = Version.Major.CompareTo(other.Version.Major);
            if (result != 0)
                return result;
            result = Version.Minor.CompareTo(other.Version.Minor);
            if (result != 0)
                return result;
            result = Version.Build.CompareTo(other.Version.Build);
            if (result != 0)
                return result;
            return Version.Revision.CompareTo(other.Version.Revision);
        }

        public static bool operator ==(PackageVersionEx x, PackageVersion y)
        {
            if (x.Version.Major == y.Major && x.Version.Minor == y.Minor && x.Version.Build == y.Build)
            {
                return x.Version.Revision == y.Revision;
            }

            return false;
        }

        public static bool operator ==(PackageVersion x, PackageVersionEx y)
        {
            if (x.Major == y.Version.Major && x.Minor == y.Version.Minor && x.Build == y.Version.Build)
            {
                return x.Revision == y.Version.Revision;
            }

            return false;
        }

        public static bool operator ==(PackageVersionEx x, PackageVersionEx y)
        {
            if (x.Version.Major == y.Version.Major && x.Version.Minor == y.Version.Minor && x.Version.Build == y.Version.Build)
            {
                return x.Version.Revision == y.Version.Revision;
            }

            return false;
        }

        public static bool operator ==(PackageVersionEx x, string y)
        {
            return x == Parse(y);
        }

        public static bool operator ==(string x, PackageVersionEx y)
        {
            return Parse(x) == y;
        }

        public static bool operator !=(PackageVersionEx x, PackageVersion y)
        {
            return !(x.Version == y);
        }

        public static bool operator !=(PackageVersion x, PackageVersionEx y)
        {
            return !(x == y.Version);
        }

        public static bool operator !=(PackageVersionEx x, PackageVersionEx y)
        {
            return !(x.Version == y.Version);
        }

        public static bool operator !=(PackageVersionEx x, string y)
        {
            return x != Parse(y);
        }

        public static bool operator !=(string x, PackageVersionEx y)
        {
            return Parse(x) != y;
        }

        public static bool operator <(PackageVersionEx left, PackageVersionEx right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(PackageVersionEx left, PackageVersionEx right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(PackageVersionEx left, PackageVersionEx right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(PackageVersionEx left, PackageVersionEx right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static implicit operator PackageVersionEx(PackageVersion version)
        {
            return new PackageVersionEx(version);
        }

        // Implicit conversion from PackageVersionEx to PackageVersion.
        public static implicit operator PackageVersion(PackageVersionEx versionEx)
        {
            return versionEx.Version;
        }

        // Implicit conversion from string to PackageVersionEx (parses the string).
        public static implicit operator PackageVersionEx(string versionStr)
        {
            return Parse(versionStr);
        }

        public bool Equals(PackageVersion other)
        {
            return Version == other;
        }

        public override bool Equals(object? obj)
        {
            if (obj is PackageVersionEx other)
            {
                return CompareTo(other) == 0;
            }
            return false;
        }

        public override int GetHashCode()
        {
            // Combine hash codes of each component.
            return Version.Major.GetHashCode() ^
                   Version.Minor.GetHashCode() ^
                   Version.Build.GetHashCode() ^
                   Version.Revision.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Version.Major}.{Version.Minor}.{Version.Build}.{Version.Revision}";
        }
    }
}
