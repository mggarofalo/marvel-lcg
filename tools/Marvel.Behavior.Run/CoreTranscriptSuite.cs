using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Behavior.Run;

internal sealed class CoreTranscriptSuite
{
    private readonly string root;
    private readonly CatalogEvidence catalog;
    private readonly CoreTranscriptRunner runner;

    public CoreTranscriptSuite(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        this.root = Path.GetFullPath(root);
        runner = new CoreTranscriptRunner(this.root);
        catalog = ReadCatalog(Path.Combine(
            this.root, "specs", "behavior", "catalog.json"));
    }

    public IReadOnlyList<TranscriptResult> RunPassingCorpus()
    {
        string directory = Path.Combine(root, "specs", "behavior", "core");
        var paths = Directory.EnumerateFiles(directory, "*.feature")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        if (paths.Count == 0)
        {
            throw new TranscriptException("specs/behavior/core contains no executable features");
        }

        var results = new List<TranscriptResult>();
        var executed = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in paths)
        {
            TranscriptFeature feature = TranscriptParser.Parse(root, path);
            foreach (TranscriptScenario scenario in feature.Scenarios)
            {
                CatalogObligation obligation = ValidateAuthority(
                    scenario, requireCompletionEvidence: true);
                string reference = Reference(scenario);
                if (!executed.Add(reference))
                {
                    throw new TranscriptException(
                        $"{scenario.Location}: duplicate executed scenario reference '{reference}'");
                }
                if (obligation.Implementation == "supported")
                {
                    results.Add(runner.Execute(scenario));
                }
                else
                {
                    results.Add(RunUnimplemented(scenario, obligation));
                }
            }
        }

        ValidateScenarioCompleteness(ExpectedScenarioReferences(), executed);

        return results;
    }

    public TranscriptResult RunScenario(string relativePath, string scenarioName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);

        string path = Path.Combine(root, relativePath);
        TranscriptScenario scenario = TranscriptParser.Parse(
            root, path, scenarioName).Scenarios.Single();
        CatalogObligation obligation = ValidateAuthority(
            scenario, requireCompletionEvidence: true);

        return obligation.Implementation == "supported"
            ? runner.Execute(scenario)
            : RunUnimplemented(scenario, obligation);
    }

    public TranscriptException RunQuarantine()
    {
        string path = Path.Combine(root, "specs", "self-test", "quarantine.feature");
        TranscriptFeature feature = TranscriptParser.Parse(
            root, path, "the executable runner rejects a false hand count");
        try
        {
            foreach (TranscriptScenario scenario in feature.Scenarios)
            {
                _ = ValidateAuthority(scenario, requireCompletionEvidence: false);
                _ = runner.Execute(scenario);
            }
        }
        catch (TranscriptException expected)
            when (expected.Kind == TranscriptFailureKind.Assertion)
        {
            return expected;
        }
        catch (TranscriptException wrongFailure)
        {
            throw new TranscriptException(
                TranscriptFailureKind.Validation,
                "quarantine did not reach its deliberately false assertion",
                wrongFailure);
        }

        throw new TranscriptException(
            "specs/self-test/quarantine.feature passed; its false assertion no longer proves the runner");
    }

    internal void ValidateForPassing(TranscriptScenario scenario) =>
        _ = ValidateAuthority(scenario, requireCompletionEvidence: true);

    private CatalogObligation ValidateAuthority(
        TranscriptScenario scenario, bool requireCompletionEvidence)
    {
        CatalogObligation obligation = ValidateNamedAuthority(
            scenario, scenario.Obligation, requireCompletionEvidence, "primary");
        foreach (string covered in scenario.CoveredObligations)
        {
            CatalogObligation secondary = ValidateNamedAuthority(
                scenario, covered, requireCompletionEvidence, "covered");
            if (!string.Equals(
                    secondary.Implementation, obligation.Implementation,
                    StringComparison.Ordinal))
            {
                throw new TranscriptException(
                    $"{scenario.Location}: covered obligation '{covered}' is "
                    + $"{secondary.Implementation}, but the primary obligation is "
                    + $"{obligation.Implementation}");
            }
        }

        if (requireCompletionEvidence)
        {
            string reference = Reference(scenario);
            var declared = scenario.CoveredObligations
                .Prepend(scenario.Obligation)
                .ToHashSet(StringComparer.Ordinal);
            var linked = catalog.Obligations.Values
                .Where(candidate => candidate.Scenarios.Contains(
                    reference, StringComparer.Ordinal))
                .Select(candidate => candidate.Id)
                .ToHashSet(StringComparer.Ordinal);
            if (!declared.SetEquals(linked))
            {
                throw new TranscriptException(
                    $"{scenario.Location}: catalog coverage differs from declared coverage; "
                    + $"declared [{string.Join(", ", declared.Order())}], linked "
                    + $"[{string.Join(", ", linked.Order())}]");
            }
        }

        return obligation;
    }

    private CatalogObligation ValidateNamedAuthority(
        TranscriptScenario scenario,
        string obligationId,
        bool requireCompletionEvidence,
        string role)
    {
        if (!catalog.Obligations.TryGetValue(
                obligationId, out CatalogObligation? obligation))
        {
            throw new TranscriptException(
                $"{scenario.Location}: stale or missing {role} obligation '{obligationId}'");
        }

        ValidateDirectAuthorities(scenario, obligation, role);
        ValidateExecutable(scenario, obligationId, obligation);
        if (!requireCompletionEvidence) return obligation;
        ValidateEvidence(scenario, obligationId, obligation);
        return obligation;
    }

    private void ValidateDirectAuthorities(
        TranscriptScenario scenario, CatalogObligation obligation, string role)
    {
        if (scenario.Authorities.Count == 0)
            throw new TranscriptException(
                $"{scenario.Location}: scenario has no direct authority tags");
        var missing = scenario.Authorities
            .Where(authority => !catalog.Sources.ContainsKey(authority)).ToList();
        if (missing.Count > 0)
            throw new TranscriptException(
                $"{scenario.Location}: missing direct authorities: {string.Join(", ", missing)}");
        var outsideCore = scenario.Authorities.Where(authority =>
                catalog.Sources.TryGetValue(authority, out CatalogSource? source)
                && source.Disposition == "outside-core").ToList();
        if (outsideCore.Count > 0)
            throw new TranscriptException(
                $"{scenario.Location}: outside-Core direct authorities: "
                + string.Join(", ", outsideCore));
        if (!scenario.Authorities.Contains(obligation.Source, StringComparer.Ordinal))
            throw new TranscriptException(
                $"{scenario.Location}: {role} obligation derives from '{obligation.Source}', "
                + "which is not a direct authority tag");
    }

    private static void ValidateExecutable(
        TranscriptScenario scenario, string obligationId,
        CatalogObligation obligation)
    {
        if (string.Equals(obligation.Disposition, "executable", StringComparison.Ordinal)
            && obligation.Implementation is "supported" or "unimplemented") return;
        throw new TranscriptException(
            $"{scenario.Location}: '{obligationId}' is "
            + $"{obligation.Disposition}/{obligation.Implementation ?? "(none)"}, "
            + "not executable with a completed implementation status");
    }

    private static void ValidateEvidence(
        TranscriptScenario scenario, string obligationId,
        CatalogObligation obligation)
    {
        string reference = Reference(scenario);
        if (!obligation.Scenarios.Contains(reference, StringComparer.Ordinal))
            throw new TranscriptException(
                $"{scenario.Location}: catalog does not link scenario '{reference}'");
        if (string.IsNullOrWhiteSpace(obligation.Mutation))
            throw new TranscriptException(
                $"{scenario.Location}: '{obligationId}' has no mutation evidence");
        if (obligation.Implementation == "unimplemented"
            && string.IsNullOrWhiteSpace(obligation.Exception))
            throw new TranscriptException(
                $"{scenario.Location}: '{obligationId}' names no expected exception");
    }

    private TranscriptResult RunUnimplemented(
        TranscriptScenario scenario, CatalogObligation obligation) =>
        runner.Execute(scenario, obligation.Exception);

    private IReadOnlySet<string> ExpectedScenarioReferences() =>
        CompletedScenarioReferences(catalog.Obligations.Values);

    internal static IReadOnlySet<string> CompletedScenarioReferences(
        IEnumerable<CatalogObligation> obligations)
    {
        var references = new HashSet<string>(StringComparer.Ordinal);
        foreach (CatalogObligation obligation in obligations.Where(obligation =>
                     obligation.Disposition == "executable"))
        {
            if (obligation.Implementation is not ("supported" or "unimplemented"))
            {
                throw new TranscriptException(
                    $"executable obligation '{obligation.Id}' is not completed; "
                    + $"implementation is {obligation.Implementation ?? "(none)"}");
            }

            if (obligation.Scenarios.Count == 0)
            {
                throw new TranscriptException(
                    $"completed obligation '{obligation.Id}' has no scenarios");
            }

            if (string.IsNullOrWhiteSpace(obligation.Mutation))
            {
                throw new TranscriptException(
                    $"completed obligation '{obligation.Id}' has no mutation evidence");
            }

            if (obligation.Implementation == "unimplemented"
                && string.IsNullOrWhiteSpace(obligation.Exception))
            {
                throw new TranscriptException(
                    $"unimplemented obligation '{obligation.Id}' names no expected exception");
            }

            foreach (string reference in obligation.Scenarios)
            {
                _ = references.Add(reference);
            }
        }

        return references;
    }

    internal static void ValidateScenarioCompleteness(
        IReadOnlySet<string> expected, IReadOnlySet<string> executed)
    {
        var missing = expected.Except(executed, StringComparer.Ordinal).ToList();
        var unexpected = executed.Except(expected, StringComparer.Ordinal).ToList();
        if (missing.Count > 0 || unexpected.Count > 0)
        {
            string details = string.Join("; ", new[]
            {
                missing.Count == 0
                    ? null
                    : $"catalog scenarios not executed: {string.Join(", ", missing)}",
                unexpected.Count == 0
                    ? null
                    : $"executed scenarios absent from catalog: {string.Join(", ", unexpected)}",
            }.Where(value => value is not null));
            throw new TranscriptException(details);
        }
    }

    private static string Reference(TranscriptScenario scenario) =>
        $"{scenario.Location.Path}::{scenario.Name}";

    private static CatalogEvidence ReadCatalog(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        var sources = new Dictionary<string, CatalogSource>(StringComparer.Ordinal);
        var obligations = new Dictionary<string, CatalogObligation>(StringComparer.Ordinal);
        foreach (JsonElement source in document.RootElement
                     .GetProperty("sources").EnumerateArray())
        {
            string sourceId = source.GetProperty("id").GetString()!;
            sources.Add(sourceId, new CatalogSource(
                source.GetProperty("disposition").GetString()!));
            foreach (JsonElement obligation in source
                         .GetProperty("obligations").EnumerateArray())
            {
                string id = obligation.GetProperty("id").GetString()!;
                obligations.Add(id, new CatalogObligation(
                    id,
                    sourceId,
                    obligation.GetProperty("disposition").GetString()!,
                    obligation.GetProperty("implementation").GetString(),
                    [.. obligation.GetProperty("scenarios").EnumerateArray()
                        .Select(item => item.GetString()!)],
                    obligation.GetProperty("mutation").GetString(),
                    obligation.GetProperty("exception").GetString()));
            }
        }

        return new CatalogEvidence(sources, obligations);
    }
}

internal sealed record CatalogObligation(
    string Id,
    string Source,
    string Disposition,
    string? Implementation,
    IReadOnlyList<string> Scenarios,
    string? Mutation,
    string? Exception);

internal sealed record CatalogSource(string Disposition);

internal sealed record CatalogEvidence(
    IReadOnlyDictionary<string, CatalogSource> Sources,
    IReadOnlyDictionary<string, CatalogObligation> Obligations);
