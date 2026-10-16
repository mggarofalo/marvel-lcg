using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Deterministic comparisons used by every journal replay consumer.</summary>
public static class JournalReplay
{
    /// <summary>Requires a freshly produced prompt to match its stable record.</summary>
    public static void RequirePrompt(PromptRecord expected, Prompt actual, string context)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        RequirePromptRecords(expected, PromptRecord.From(actual), context);
    }

    /// <summary>Compares optional recorded prompts using their schema's recorded fields.</summary>
    public static void RequirePromptRecords(PromptRecord? expected, PromptRecord? actual, string context)
    {
        RequireEqual(
            JsonSerializer.Serialize(expected, JournalJson.Options),
            JsonSerializer.Serialize(actual, JournalJson.Options),
            context);
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
