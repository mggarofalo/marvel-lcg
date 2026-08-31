using System.Text.Json;
using System.Text.RegularExpressions;

namespace Marvel.MarvelCdb.Harvest;

public static partial class Harvest
{
    public const int BatchSize = 250;

    public static string Version(ICommandRunner runner)
    {
        CommandResult result = runner.Run(["version"]);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"`marvelcdb version` failed ({result.ExitCode}): {result.Error.Trim()}");
        }

        return result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim()).FirstOrDefault()
            ?? throw new InvalidDataException("`marvelcdb version` returned no version");
    }

    public static IReadOnlyList<string> Codes(ICommandRunner runner)
    {
        CommandResult result = runner.Run(
            ["cards", "list", "--encounter", "--duplicates", "-o", "ids"]);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"`marvelcdb cards list` failed ({result.ExitCode}): {result.Error.Trim()}");
        }

        var codes = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(code => code.Trim()).Where(code => code.Length > 0)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        return codes.Count > 0
            ? codes
            : throw new InvalidDataException("`marvelcdb cards list` returned no codes");
    }

    public static IReadOnlyList<JsonElement> Batch(
        ICommandRunner runner,
        IReadOnlyList<string> codes)
    {
        var arguments = new List<string> { "faq" };
        arguments.AddRange(codes);
        arguments.AddRange(["-o", "json"]);
        CommandResult result = runner.Run(arguments);

        if (result.ExitCode == 4)
        {
            throw new InvalidDataException(
                "marvelcdb reports an unknown code from its own card list; rerun the harvest");
        }

        if (result.ExitCode == 5)
        {
            throw new InvalidOperationException("marvelcdb has no network or cached response");
        }

        IReadOnlyList<JsonElement> entries = ParseEntries(result.Output);
        var asked = codes.ToHashSet(StringComparer.Ordinal);
        var unexpected = entries.Select(entry => entry.GetProperty("code").GetString()!)
            .Where(code => !asked.Contains(code)).Distinct(StringComparer.Ordinal).ToList();
        if (unexpected.Count > 0)
        {
            throw new InvalidDataException(
                "marvelcdb returned FAQ entries that were not requested: "
                + string.Join(", ", unexpected));
        }

        var answered = entries.Select(entry => entry.GetProperty("code").GetString()!)
            .Concat(EmptyCodes(result.Error)).ToHashSet(StringComparer.Ordinal);
        var missing = codes.Where(code => !answered.Contains(code)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidDataException(
                $"{missing.Count} code(s) came back in neither stdout nor stderr: "
                + string.Join(", ", missing.Take(10))
                + (missing.Count > 10 ? " ..." : string.Empty));
        }

        return entries;
    }

    public static Snapshot All(
        ICommandRunner runner,
        string harvested,
        int limit = 0,
        Action<int, int, int>? progress = null)
    {
        string version = Version(runner);
        var codes = Codes(runner).ToList();
        if (limit > 0)
        {
            codes = codes.Take(limit).ToList();
        }

        var entries = new List<JsonElement>();
        for (int start = 0; start < codes.Count; start += BatchSize)
        {
            var batch = codes.Skip(start).Take(BatchSize).ToList();
            entries.AddRange(Batch(runner, batch));
            progress?.Invoke(Math.Min(start + BatchSize, codes.Count), codes.Count, entries.Count);
        }

        return new Snapshot(harvested, version, codes, entries, limit == 0);
    }

    public static IReadOnlyList<JsonElement> ParseEntries(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        using var document = JsonDocument.Parse(output);
        return document.RootElement.ValueKind switch
        {
            JsonValueKind.Object => [document.RootElement.Clone()],
            JsonValueKind.Array => document.RootElement.EnumerateArray()
                .Select(entry => entry.Clone()).ToList(),
            _ => throw new InvalidDataException(
                $"unexpected JSON from `marvelcdb faq`: {document.RootElement.ValueKind}"),
        };
    }

    public static IReadOnlyList<string> EmptyCodes(string error) =>
        error.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => NoEntries().Match(line.Trim()))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .ToList();

    [GeneratedRegex("^no FAQ entries for (\\S+)$", RegexOptions.CultureInvariant)]
    private static partial Regex NoEntries();
}
