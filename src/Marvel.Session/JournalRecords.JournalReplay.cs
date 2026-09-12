using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

/// <summary>Deterministic comparisons used by every journal replay consumer.</summary>
public static class JournalReplay
{
    /// <summary>Requires a freshly produced prompt to match its stable record.</summary>
    public static void RequirePrompt(PromptRecord expected, Prompt actual, string context)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        PromptRecord recorded = NormalizeUnrecordedSchemaTwoFields(
            expected, PromptRecord.From(actual));
        RequireEqual(
            JsonSerializer.Serialize(expected, JournalJson.Options),
            JsonSerializer.Serialize(recorded, JournalJson.Options),
            context);
    }

    private static PromptRecord NormalizeUnrecordedSchemaTwoFields(
        PromptRecord expected, PromptRecord actual)
    {
        if (expected.Affordances is null
            || expected.Affordances.Count != actual.Affordances.Count)
        {
            return actual;
        }

        var affordances = new List<AffordanceRecord>(actual.Affordances.Count);
        for (int index = 0; index < actual.Affordances.Count; index++)
        {
            if (expected.Affordances[index] is not AffordanceRecord expectedAffordance
                || expectedAffordance.Costs is null)
            {
                return actual;
            }

            AffordanceRecord actualAffordance = actual.Affordances[index];
            affordances.Add(actualAffordance with
            {
                Targets = NormalizeTargets(expectedAffordance.Targets, actualAffordance.Targets),
                Costs = NormalizeCosts(expectedAffordance.Costs, actualAffordance.Costs),
            });
        }

        return actual with { Affordances = affordances };
    }

    private static TargetRequestRecord? NormalizeTargets(
        TargetRequestRecord? expected, TargetRequestRecord? actual)
    {
        if (expected is null || actual is null)
        {
            return actual;
        }
        return actual with
        {
            AllowRepeated = expected.LegacyAllowRepeatedRecorded
                ? actual.AllowRepeated : expected.AllowRepeated,
            MaximumOccurrences = expected.LegacyMaximumOccurrencesRecorded
                ? actual.MaximumOccurrences : expected.MaximumOccurrences,
            Details = expected.LegacyDetailsRecorded ? actual.Details : expected.Details,
        };
    }

    private static IReadOnlyList<CostOptionRecord> NormalizeCosts(
        IReadOnlyList<CostOptionRecord> expected,
        IReadOnlyList<CostOptionRecord> actual)
    {
        if (expected.Count != actual.Count)
        {
            return actual;
        }
        return [.. actual.Select((cost, index) =>
            expected[index].LegacyDeclarationSensitiveRecorded
                ? cost
                : cost with { DeclarationSensitive = expected[index].DeclarationSensitive })];
    }

    /// <summary>Requires semantic events to retain their exact count, order and shape.</summary>
    public static void RequireEvents(
        IReadOnlyList<JsonElement> expected,
        IReadOnlyList<GameEvent> actual,
        string context)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        var recorded = actual.Select(JournalJson.Event).ToList();
        if (expected.Count != recorded.Count)
        {
            throw new ReplayDivergenceException(
                $"{context} diverged: expected {expected.Count}, got {recorded.Count}");
        }

        for (int index = 0; index < expected.Count; index++)
        {
            RequireEqual(
                expected[index].GetRawText(),
                recorded[index].GetRawText(),
                $"{context}[{index}]");
        }
    }

    /// <summary>Requires the exact hidden-state fingerprint produced after a decision.</summary>
    public static void RequireFingerprint(string expected, string actual, string context) =>
        RequireEqual(expected, actual, context);

    /// <summary>Requires cumulative gameplay RNG consumption to remain exact.</summary>
    public static void RequireRng(long expected, long actual, string context) =>
        RequireEqual(expected, actual, context);

    private static void RequireEqual<T>(T expected, T actual, string context)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new ReplayDivergenceException(
                $"{context} diverged: expected '{expected}', got '{actual}'");
        }
    }
}
