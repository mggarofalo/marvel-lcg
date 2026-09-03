using System.Globalization;
using System.Text.RegularExpressions;

namespace Marvel.Release;

internal enum ReleaseChannel
{
    Developer,
    Preview,
    Stable,
}

internal sealed partial record ReleaseVersion(
    int Major,
    int Minor,
    int Patch,
    ReleaseChannel Channel,
    int? Sequence,
    string Value)
{
    [GeneratedRegex(
        "^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)(?:-(preview|dev)\\.(0|[1-9][0-9]*))?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    public string AssemblyVersion =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{Major}.{Minor}.{Patch}.0");

    public string MsixVersion => Channel switch
    {
        ReleaseChannel.Preview when Sequence is >= 1 and <= 65534 =>
            string.Create(
                CultureInfo.InvariantCulture,
                $"{Major + 1}.{Minor}.{Patch}.{Sequence}"),
        ReleaseChannel.Stable => string.Create(
            CultureInfo.InvariantCulture,
            $"{Major + 1}.{Minor}.{Patch}.65535"),
        ReleaseChannel.Developer => throw new InvalidOperationException(
            "developer builds do not use the release MSIX identity"),
        _ => throw new InvalidOperationException(
            "preview sequence must be from 1 through 65534"),
    };

    public static ReleaseVersion Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Match match = Pattern().Match(value);
        if (!match.Success)
        {
            throw new ArgumentException(
                "version must be MAJOR.MINOR.PATCH, MAJOR.MINOR.PATCH-preview.N, or MAJOR.MINOR.PATCH-dev.N");
        }

        int major = Component(match.Groups[1].Value, "major");
        int minor = Component(match.Groups[2].Value, "minor");
        int patch = Component(match.Groups[3].Value, "patch");
        if (major > 65534)
        {
            throw new ArgumentException("version major must be at most 65534");
        }

        string prerelease = match.Groups[4].Value;
        int? sequence = match.Groups[5].Success
            ? Component(match.Groups[5].Value, "prerelease sequence")
            : null;
        ReleaseChannel channel = prerelease switch
        {
            "preview" => ReleaseChannel.Preview,
            "dev" => ReleaseChannel.Developer,
            "" => ReleaseChannel.Stable,
            _ => throw new ArgumentException("unsupported release channel"),
        };
        if (channel == ReleaseChannel.Preview && sequence is not (>= 1 and <= 65534))
        {
            throw new ArgumentException("preview sequence must be from 1 through 65534");
        }

        return new ReleaseVersion(major, minor, patch, channel, sequence, value);
    }

    private static int Component(string value, string name)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed)
            || parsed > ushort.MaxValue)
        {
            throw new ArgumentException($"version {name} must be from 0 through 65535");
        }

        return parsed;
    }
}
