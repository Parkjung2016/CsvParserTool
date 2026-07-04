using System;
using System.Globalization;

namespace CSVParserTool
{
    /// <summary>테이블 컬럼·Export 버전 (<c>major.minor.patch</c>).</summary>
    public readonly struct DataVersion : IComparable<DataVersion>, IEquatable<DataVersion>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }

        public DataVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public static bool TryParse(string raw, out DataVersion version)
        {
            version = default;
            raw = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(raw))
                return false;

            string[] parts = raw.Split('.');
            if (parts.Length < 1 || parts.Length > 3)
                return false;

            if (!TryParsePart(parts[0], out int major) || major < 0)
                return false;

            int minor = 0;
            int patch = 0;
            if (parts.Length >= 2 && !TryParsePart(parts[1], out minor))
                return false;
            if (parts.Length >= 3 && !TryParsePart(parts[2], out patch))
                return false;

            version = new DataVersion(major, minor, patch);
            return true;
        }

        public int CompareTo(DataVersion other)
        {
            int c = Major.CompareTo(other.Major);
            if (c != 0)
                return c;
            c = Minor.CompareTo(other.Minor);
            if (c != 0)
                return c;
            return Patch.CompareTo(other.Patch);
        }

        public bool Equals(DataVersion other) => CompareTo(other) == 0;

        public override bool Equals(object obj) => obj is DataVersion other && Equals(other);

        public override int GetHashCode() => Major ^ (Minor << 8) ^ (Patch << 16);

        public override string ToString() => $"{Major}.{Minor}.{Patch}";

        public static bool operator ==(DataVersion left, DataVersion right) => left.Equals(right);

        public static bool operator !=(DataVersion left, DataVersion right) => !left.Equals(right);

        public static bool operator <=(DataVersion left, DataVersion right) => left.CompareTo(right) <= 0;

        public static bool operator >=(DataVersion left, DataVersion right) => left.CompareTo(right) >= 0;

        public static bool operator <(DataVersion left, DataVersion right) => left.CompareTo(right) < 0;

        public static bool operator >(DataVersion left, DataVersion right) => left.CompareTo(right) > 0;

        private static bool TryParsePart(string part, out int value) =>
            int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }
}
