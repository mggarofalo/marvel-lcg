using Gherkin;
using Gherkin.Ast;

namespace Marvel.Behavior.Run;

internal static class TranscriptParser
{
    public static TranscriptFeature Parse(
        string root, string path, string? onlyScenario = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string relative = Path.GetRelativePath(root, path).Replace('\\', '/');
        GherkinDocument document;
        try
        {
            document = new Parser().Parse(path);
        }
        catch (Exception error) when (error is CompositeParserException or ParserException)
        {
            throw new TranscriptException(
                TranscriptFailureKind.Validation, $"{relative}: {error.Message}", error);
        }

        Feature feature = document.Feature
            ?? throw new TranscriptException($"{relative}:1:1: feature is missing");
        var backgrounds = feature.Children.OfType<Background>().ToList();
        if (backgrounds.Count > 1)
        {
            throw At(relative, backgrounds[1].Location, "a feature may have only one Background");
        }

        var prefix = backgrounds.Count == 0
            ? Array.Empty<Step>()
            : backgrounds[0].Steps.ToArray();
        var scenarios = new List<TranscriptScenario>();
        foreach (Scenario scenario in feature.Children.OfType<Scenario>()
                     .Where(scenario => onlyScenario is null
                         || string.Equals(scenario.Name, onlyScenario, StringComparison.Ordinal)))
        {
            if (scenario.Examples.Any())
            {
                throw At(relative, scenario.Location,
                    "Scenario Outline is not part of the canonical transcript format");
            }

            IReadOnlyList<string> tags = [.. feature.Tags.Concat(scenario.Tags)
                .Select(tag => tag.Name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(tag => tag, StringComparer.Ordinal)];
            var obligations = tags
                .Where(tag => tag.StartsWith("@behavior:", StringComparison.Ordinal))
                .Select(tag => tag[1..])
                .ToList();
            if (obligations.Count != 1)
            {
                throw At(relative, scenario.Location,
                    $"scenario must name exactly one @behavior: obligation; found {obligations.Count}");
            }

            var parsed = ParseSteps(relative, prefix.Concat(scenario.Steps));
            scenarios.Add(new TranscriptScenario(
                scenario.Name,
                obligations[0],
                [.. tags.Where(IsAuthority).Select(tag => tag[1..])],
                parsed,
                Locate(relative, scenario.Location)));
        }

        if (scenarios.Count == 0)
        {
            throw At(relative, feature.Location, "feature has no scenarios");
        }

        return new TranscriptFeature(
            feature.Name, scenarios, Locate(relative, feature.Location));
    }

    private static List<TranscriptStep> ParseSteps(
        string path, IEnumerable<Step> source)
    {
        var parsed = new List<TranscriptStep>();
        TranscriptStepKind? preceding = null;
        bool hasDecision = false;
        bool decisionObserved = true;
        foreach (Step step in source)
        {
            string keyword = step.Keyword.Trim();
            TranscriptStepKind kind = keyword switch
            {
                "Given" => TranscriptStepKind.Given,
                "When" => TranscriptStepKind.When,
                "Then" => TranscriptStepKind.Then,
                "And" or "But" when preceding is not null => preceding.Value,
                "And" or "But" => throw At(path, step.Location,
                    $"{keyword} has no preceding step kind"),
                _ => throw At(path, step.Location,
                    $"unsupported step keyword '{keyword}'"),
            };

            if (kind == TranscriptStepKind.Given && hasDecision)
            {
                throw At(path, step.Location, "Given cannot appear after the first When");
            }

            if (kind == TranscriptStepKind.Then && !hasDecision)
            {
                throw At(path, step.Location, "Then requires a preceding When");
            }

            if (kind == TranscriptStepKind.When)
            {
                if (!decisionObserved)
                {
                    throw At(path, step.Location,
                        "When cannot follow an unobserved decision; add a Then first");
                }

                hasDecision = true;
                decisionObserved = false;
            }

            if (kind == TranscriptStepKind.Then)
            {
                decisionObserved = true;
            }

            if (step.Argument is DocString)
            {
                throw At(path, step.Location,
                    "doc strings are not consumed by the canonical step vocabulary");
            }

            parsed.Add(new TranscriptStep(
                kind, step.Text, ParseTable(path, step), Locate(path, step.Location)));
            preceding = kind;
        }

        if (hasDecision && !decisionObserved)
        {
            TranscriptLocation location = parsed[^1].Location;
            throw new TranscriptException(
                $"{location}: the final When has no observable Then");
        }

        if (!hasDecision)
        {
            TranscriptLocation location = parsed.Count == 0
                ? new TranscriptLocation(path, 1, 1)
                : parsed[^1].Location;
            throw new TranscriptException(
                $"{location}: an executable scenario requires a When decision");
        }

        return parsed;
    }

    private static TranscriptTable? ParseTable(string path, Step step)
    {
        if (step.Argument is not DataTable dataTable)
        {
            return null;
        }

        var rows = dataTable.Rows.ToList();
        if (rows.Count < 2)
        {
            throw At(path, step.Location, "a step table needs a header and at least one row");
        }

        IReadOnlyList<string> header = [.. rows[0].Cells.Select(cell => cell.Value)];
        if (header.Any(string.IsNullOrWhiteSpace)
            || header.Distinct(StringComparer.Ordinal).Count() != header.Count)
        {
            throw At(path, rows[0].Location,
                "table headers must be non-empty and unique");
        }

        var values = new List<IReadOnlyDictionary<string, string>>();
        foreach (TableRow row in rows.Skip(1))
        {
            var cells = row.Cells.ToList();
            if (cells.Count != header.Count)
            {
                throw At(path, row.Location,
                    $"table row has {cells.Count} cells; header has {header.Count}");
            }

            values.Add(header.Select((name, index) => (name, cells[index].Value))
                .ToDictionary(pair => pair.name, pair => pair.Value, StringComparer.Ordinal));
        }

        return new TranscriptTable(header, values);
    }

    private static bool IsAuthority(string tag) =>
        tag.StartsWith("@rr:", StringComparison.Ordinal)
        || tag.StartsWith("@card:", StringComparison.Ordinal)
        || tag.StartsWith("@ruling:", StringComparison.Ordinal)
        || tag.StartsWith("@faq:", StringComparison.Ordinal)
        || tag.StartsWith("@setup:", StringComparison.Ordinal);

    private static TranscriptLocation Locate(string path, Location location) =>
        new(path, location.Line, location.Column);

    private static TranscriptException At(string path, Location location, string message) =>
        new($"{Locate(path, location)}: {message}");
}
