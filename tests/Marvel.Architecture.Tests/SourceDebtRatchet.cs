using System.Globalization;
using System.Text.RegularExpressions;

namespace Marvel.Architecture.Tests;

/// <summary>Compares reviewed debt by identity and magnitude, independent of source line movement.</summary>
internal static class SourceDebtRatchet
{
    internal static string[] Increases(IEnumerable<string> previous, IEnumerable<string> current)
    {
        Dictionary<string, int[]> admitted = Measurements(previous);
        var failures = new List<string>();
        foreach ((string key, int[] values) in Measurements(current))
        {
            int[] limits = admitted.GetValueOrDefault(key) ?? [];
            for (int index = 0; index < values.Length; index++)
                if (index >= limits.Length || values[index] > limits[index])
                    failures.Add($"{key}={values[index]} exceeds reviewed debt");
        }
        return failures.ToArray();
    }

    private static Dictionary<string, int[]> Measurements(IEnumerable<string> lines) => lines
        .Select(Parse).GroupBy(item => item.Key, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.Select(item => item.Value)
            .OrderDescending().ToArray(), StringComparer.Ordinal);

    private static KeyValuePair<string, int> Parse(string line)
    {
        Match match = Regex.Match(line, @"^(.*):\d+: (SL003|SL005): (.*)=(\d+)$",
            RegexOptions.CultureInvariant);
        if (!match.Success) throw new InvalidOperationException($"Unrecognized source debt: {line}");
        return new($"{match.Groups[1].Value}: {match.Groups[2].Value}: {match.Groups[3].Value}",
            int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture));
    }
}
