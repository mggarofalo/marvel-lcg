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
        Feature feature = ParseDocument(path, relative).Feature
            ?? throw new TranscriptException($"{relative}:1:1: feature is missing");
        var backgrounds = feature.Children.OfType<Background>().ToList();
        if (backgrounds.Count > 1)
            throw At(relative, backgrounds[1].Location, "a feature may have only one Background");
        var prefix = backgrounds.Count == 0
            ? Array.Empty<Step>()
            : backgrounds[0].Steps.ToArray();
        var scenarios = new List<TranscriptScenario>();
        foreach (Scenario scenario in feature.Children.OfType<Scenario>()
                     .Where(scenario => onlyScenario is null
                         || string.Equals(scenario.Name, onlyScenario, StringComparison.Ordinal)))
        {
            scenarios.Add(ParseScenario(relative, feature, scenario, prefix));
        }
        ValidateScenarios(relative, feature, scenarios);
        return new TranscriptFeature(
            feature.Name, scenarios, Locate(relative, feature.Location));
    }

    private static GherkinDocument ParseDocument(string path, string relative)
    {
        try
        {
            return new Parser().Parse(path);
        }
        catch (Exception error) when (error is CompositeParserException or ParserException)
        {
            throw new TranscriptException(
                TranscriptFailureKind.Validation, $"{relative}: {error.Message}", error);
        }
    }

    private static TranscriptScenario ParseScenario(
        string path, Feature feature, Scenario scenario, IReadOnlyList<Step> prefix)
    {
        if (scenario.Examples.Any())
            throw At(path, scenario.Location,
                "Scenario Outline is not part of the canonical transcript format");
        IReadOnlyList<string> tags = [.. feature.Tags.Concat(scenario.Tags)
            .Select(tag => tag.Name).Distinct(StringComparer.Ordinal)
            .OrderBy(tag => tag, StringComparer.Ordinal)];
        var obligations = tags.Where(tag =>
                tag.StartsWith("@behavior:", StringComparison.Ordinal))
            .Select(tag => tag[1..]).ToList();
        if (obligations.Count != 1)
            throw At(path, scenario.Location,
                $"scenario must name exactly one @behavior: obligation; found {obligations.Count}");
        var covered = tags.Where(tag =>
                tag.StartsWith("@covers:behavior:", StringComparison.Ordinal))
            .Select(tag => tag[8..]).ToList();
        if (covered.Contains(obligations[0], StringComparer.Ordinal))
            throw At(path, scenario.Location,
                $"primary obligation '{obligations[0]}' cannot also be a @covers obligation");
        return new TranscriptScenario(
            scenario.Name, obligations[0], covered,
            [.. tags.Where(IsAuthority).Select(tag => tag[1..])],
            ParseSteps(path, prefix.Concat(scenario.Steps)),
            Locate(path, scenario.Location));
    }

    private static void ValidateScenarios(
        string path, Feature feature, List<TranscriptScenario> scenarios)
    {
        if (scenarios.Count == 0)
            throw At(path, feature.Location, "feature has no scenarios");
        var duplicate = scenarios
            .GroupBy(scenario => scenario.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            TranscriptScenario repeated = duplicate.Skip(1).First();
            throw new TranscriptException(
                $"{repeated.Location}: duplicate scenario name '{duplicate.Key}'");
        }
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
            TranscriptStepKind kind = StepKind(path, step, keyword, preceding);
            ValidateTransition(path, step, kind, hasDecision, decisionObserved);
            if (kind == TranscriptStepKind.When)
            {
                hasDecision = true;
                decisionObserved = false;
            }
            if (kind == TranscriptStepKind.Then) decisionObserved = true;
            if (step.Argument is DocString)
                throw At(path, step.Location,
                    "doc strings are not consumed by the canonical step vocabulary");
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

    private static TranscriptStepKind StepKind(
        string path, Step step, string keyword, TranscriptStepKind? preceding) =>
        keyword switch
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

    private static void ValidateTransition(
        string path, Step step, TranscriptStepKind kind,
        bool hasDecision, bool decisionObserved)
    {
        if (kind == TranscriptStepKind.Given && hasDecision)
            throw At(path, step.Location, "Given cannot appear after the first When");
        if (kind == TranscriptStepKind.Then && !hasDecision)
            throw At(path, step.Location, "Then requires a preceding When");
        if (kind == TranscriptStepKind.When && !decisionObserved)
            throw At(path, step.Location,
                "When cannot follow an unobserved decision; add a Then first");
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
