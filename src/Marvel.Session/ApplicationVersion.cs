using System.Globalization;
using System.Text.RegularExpressions;

namespace Marvel.Session;

/// <summary>A supported product SemVer used only for save compatibility ordering.</summary>
internal sealed record ApplicationVersion(
    int Major,
    int Minor,
    int Patch,
    IReadOnlyList<string> Prerelease) : IComparable<ApplicationVersion>
{
    private static readonly Regex Pattern = new(
        "^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*))?(?:\\+[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*)?$",
        RegexOptions.CultureInvariant);

    public static ApplicationVersion Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Match match = Pattern.Match(value);
        if (!match.Success
            || !int.TryParse(match.Groups[1].Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out int major)
            || !int.TryParse(match.Groups[2].Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out int minor)
            || !int.TryParse(match.Groups[3].Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out int patch))
        {
            throw new SessionSaveException("save application version is not supported SemVer");
        }

        string prerelease = match.Groups[4].Value;
        string[] identifiers = prerelease.Length == 0 ? [] : prerelease.Split('.');
        if (identifiers.Any(identifier => identifier.Length > 1
            && identifier[0] == '0'
            && identifier.All(char.IsAsciiDigit)))
        {
            throw new SessionSaveException("save application version is not supported SemVer");
        }

        return new ApplicationVersion(
            major,
            minor,
            patch,
            identifiers);
    }

    public int CompareTo(ApplicationVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        int core = CompareCore(other);
        if (core != 0) return core;
        if (Prerelease.Count == 0 || other.Prerelease.Count == 0)
        {
            return Prerelease.Count == other.Prerelease.Count
                ? 0
                : Prerelease.Count == 0 ? 1 : -1;
        }

        for (int index = 0; index < Math.Min(Prerelease.Count, other.Prerelease.Count); index++)
        {
            int part = ComparePrerelease(Prerelease[index], other.Prerelease[index]);
            if (part != 0) return part;
        }

        return Prerelease.Count.CompareTo(other.Prerelease.Count);
    }

    private int CompareCore(ApplicationVersion other)
    {
        int result = Major.CompareTo(other.Major);
        result = result == 0 ? Minor.CompareTo(other.Minor) : result;
        return result == 0 ? Patch.CompareTo(other.Patch) : result;
    }

    private static int ComparePrerelease(string left, string right)
    {
        bool leftNumeric = left.All(char.IsAsciiDigit);
        bool rightNumeric = right.All(char.IsAsciiDigit);
        if (leftNumeric && rightNumeric)
        {
            return CompareNumeric(left, right);
        }
        if (leftNumeric || rightNumeric)
        {
            return leftNumeric ? -1 : 1;
        }
        return string.Compare(left, right, StringComparison.Ordinal);
    }

    private static int CompareNumeric(string left, string right)
    {
        int length = left.Length.CompareTo(right.Length);
        return length != 0 ? length : string.Compare(left, right, StringComparison.Ordinal);
    }
}
