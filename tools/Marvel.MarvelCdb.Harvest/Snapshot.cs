using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Marvel.MarvelCdb.Harvest;

public sealed record Snapshot(
    string Harvested,
    string Harvester,
    IReadOnlyList<string> Queried,
    IReadOnlyList<JsonElement> Entries,
    bool CandidateComplete = false,
    IReadOnlyList<QueryOutcome>? Outcomes = null)
{
    private static readonly string[] FaceSuffixes = ["a", "b", "c"];

    public static Snapshot Read(string json)
    {
        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (root.GetProperty("version").GetInt32() != 1)
        {
            throw new InvalidDataException("unsupported MarvelCDB FAQ snapshot version");
        }

        var queried = root.GetProperty("queried").EnumerateArray()
            .Select(code => code.GetString()
                ?? throw new InvalidDataException("a queried card code is null"))
            .ToList();
        if (queried.Count != queried.Distinct(StringComparer.Ordinal).Count())
        {
            throw new InvalidDataException("queried card codes must be unique");
        }

        var entries = root.GetProperty("entries").EnumerateArray()
            .Select(entry => entry.Clone()).ToList();
        var asked = queried.ToHashSet(StringComparer.Ordinal);
        foreach (JsonElement entry in entries)
        {
            string code = entry.GetProperty("code").GetString()
                ?? throw new InvalidDataException("a FAQ entry has no card code");
            if (!asked.Contains(code))
            {
                throw new InvalidDataException(
                    $"FAQ entry {code} was not recorded in the queried set");
            }
        }

        IReadOnlyList<QueryOutcome>? outcomes = root.TryGetProperty("outcomes", out JsonElement observed)
            ? observed.EnumerateArray().Select(outcome => new QueryOutcome(
                outcome.GetProperty("code").GetString()
                    ?? throw new InvalidDataException("an outcome has no card code"),
                outcome.GetProperty("result").GetString()
                    ?? throw new InvalidDataException("an outcome has no result"))).ToList()
            : null;
        return new Snapshot(
            root.GetProperty("harvested").GetString()
                ?? throw new InvalidDataException("the harvest date is missing"),
            root.GetProperty("harvester").GetString()
                ?? throw new InvalidDataException("the harvester version is missing"),
            queried,
            entries,
            root.TryGetProperty("candidate_complete", out JsonElement complete)
                && complete.ValueKind == JsonValueKind.True,
            outcomes);
    }

    public string Json() => Render(candidate: false);

    public string CandidateJson() => Render(candidate: true);

    public void VerifyPublishable()
    {
        if (!CandidateComplete)
        {
            throw new InvalidDataException(
                "the candidate is partial or lacks completion accounting; run a full `fetch`");
        }

        if (Outcomes is null)
        {
            throw new InvalidDataException("the candidate has no per-code acquisition outcomes");
        }

        if (Outcomes.Select(outcome => outcome.Code).Distinct(StringComparer.Ordinal).Count()
            != Outcomes.Count)
        {
            throw new InvalidDataException("candidate outcomes contain a repeated card code");
        }

        var outcomes = Outcomes.ToDictionary(outcome => outcome.Code, StringComparer.Ordinal);
        if (!outcomes.Keys.ToHashSet(StringComparer.Ordinal)
                .SetEquals(Queried))
        {
            throw new InvalidDataException(
                "candidate outcomes must account exactly once for every queried code");
        }

        var entryCodes = Entries.Select(EntryCode).ToHashSet(StringComparer.Ordinal);
        foreach ((string code, QueryOutcome outcome) in outcomes)
        {
            bool hasEntry = entryCodes.Contains(code);
            if (outcome.Result is not ("entry" or "none")
                || (outcome.Result == "entry") != hasEntry)
            {
                throw new InvalidDataException(
                    $"candidate outcome for {code} does not match its FAQ entries");
            }
        }
    }

    public IReadOnlyDictionary<string, JsonElement> FirstEntries(out IReadOnlyList<string> duplicates)
    {
        var first = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var repeated = new List<string>();
        foreach (JsonElement entry in Entries)
        {
            string code = EntryCode(entry);
            if (!first.TryAdd(code, entry))
            {
                repeated.Add(code);
            }
        }

        duplicates = repeated;
        return first;
    }

    public static IReadOnlyList<string> Faces(string code, IReadOnlySet<string> cardIds)
    {
        if (cardIds.Contains(code))
        {
            return [code];
        }

        return FaceSuffixes.Select(suffix => code + suffix)
            .Where(cardIds.Contains).ToList();
    }

    public IReadOnlyDictionary<string, IReadOnlyList<JsonElement>> ByCard(
        IReadOnlySet<string> cardIds)
    {
        IReadOnlyDictionary<string, JsonElement> entries = FirstEntries(out _);
        var found = new Dictionary<string, List<JsonElement>>(StringComparer.Ordinal);
        foreach ((string code, JsonElement entry) in entries.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            foreach (string cardId in Faces(code, cardIds))
            {
                if (!found.TryGetValue(cardId, out List<JsonElement>? rulings))
                {
                    rulings = [];
                    found[cardId] = rulings;
                }

                rulings.Add(entry);
            }
        }

        return found.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<JsonElement>)pair.Value,
            StringComparer.Ordinal);
    }

    public IReadOnlyList<string> Unmapped(IReadOnlySet<string> cardIds) =>
        FirstEntries(out _).Keys.Where(code => Faces(code, cardIds).Count == 0)
            .Order(StringComparer.Ordinal).ToList();

    public bool WasAsked(string cardId)
    {
        var asked = Queried.ToHashSet(StringComparer.Ordinal);
        return asked.Contains(cardId)
            || (cardId.Length > 0
                && cardId[^1] is 'a' or 'b' or 'c'
                && asked.Contains(cardId[..^1]));
    }

    private string Render(bool candidate)
    {
        var lines = new List<string>
        {
            "{",
            "\"version\": 1,",
        };
        if (candidate)
        {
            lines.Add($"\"candidate_complete\": {(CandidateComplete ? "true" : "false")},");
            lines.Add("\"outcomes\": [");
            var outcomes = (Outcomes ?? []).OrderBy(outcome => outcome.Code, StringComparer.Ordinal).ToList();
            lines.AddRange(outcomes.Select((outcome, index) =>
                $"{{\"code\":{CanonicalJson.String(outcome.Code)},\"result\":{CanonicalJson.String(outcome.Result)}}}"
                + (index + 1 < outcomes.Count ? "," : string.Empty)));
            lines.Add("],");
        }

        lines.Add($"\"harvested\": {CanonicalJson.String(Harvested)},");
        lines.Add("\"source\": \"https://marvelcdb.com\",");
        lines.Add($"\"harvester\": {CanonicalJson.String(Harvester)},");
        lines.Add("\"note\": \"Raw MarvelCDB FAQ entries, verbatim. `queried` is every code asked about, so a code absent from `entries` but present in `queried` has no ruling rather than an unknown one. Vendored, not generated: see UPSTREAM.md.\",");
        lines.Add("\"queried\": [");

        var queried = Queried.OrderBy(code => code, StringComparer.Ordinal).ToList();
        lines.AddRange(queried.Select((code, index) =>
            CanonicalJson.String(code) + (index + 1 < queried.Count ? "," : "")));
        lines.Add("],");
        lines.Add("\"entries\": [");

        var entries = Entries.OrderBy(EntryCode, StringComparer.Ordinal).ToList();
        lines.AddRange(entries.Select((entry, index) =>
            CanonicalJson.Entry(entry) + (index + 1 < entries.Count ? "," : "")));
        lines.Add("]");
        lines.Add("}");
        return string.Join('\n', lines) + "\n";
    }

    private static string EntryCode(JsonElement entry) =>
        entry.GetProperty("code").GetString() ?? string.Empty;
}

public sealed record QueryOutcome(string Code, string Result);

internal static partial class CanonicalJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string String(string value) => JsonSerializer.Serialize(value, Options);

    public static string Entry(JsonElement entry)
    {
        string[] first = ["code", "html", "text", "updated"];
        var properties = entry.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value,
            StringComparer.Ordinal);
        var names = first.Where(properties.ContainsKey)
            .Concat(properties.Keys.Except(first, StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal));
        return "{" + string.Join(", ", names.Select(name =>
            $"{String(name)}: {Element(properties[name])}")) + "}";
    }

    private static string Element(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(", ", element.EnumerateObject().Select(
            property => $"{String(property.Name)}: {Element(property.Value)}")) + "}",
        JsonValueKind.Array => "[" + string.Join(", ", element.EnumerateArray().Select(Element)) + "]",
        JsonValueKind.String => String(element.GetString() ?? string.Empty),
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "null",
        _ => throw new InvalidDataException($"unsupported JSON value {element.ValueKind}"),
    };
}
