using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Replay identities selected by this engine build and its datasets.</summary>
public sealed record SessionCompatibility(
    [property: JsonRequired] string Application,
    [property: JsonRequired] string ReplayContract,
    [property: JsonRequired] string RngContract,
    [property: JsonRequired] string StateDigest,
    [property: JsonRequired] string CardsSha256,
    [property: JsonRequired] string SetupSha256,
    [property: JsonRequired] string AbilitiesSha256);

/// <summary>The complete deterministic input from which a game is dealt.</summary>
public sealed record SessionSetup(
    [property: JsonRequired] string Scenario,
    [property: JsonRequired] IReadOnlyList<string> Heroes,
    [property: JsonRequired] IReadOnlyList<string>? ModularSets,
    [property: JsonRequired] uint Seed);

/// <summary>The non-game identity and durable lifecycle of one hosted table.</summary>
public sealed record SessionIdentity(
    [property: JsonRequired] string StorageId,
    [property: JsonRequired] string Label,
    [property: JsonRequired] string Lifecycle);

/// <summary>Setup output that replay must reproduce before accepting decisions.</summary>
public sealed record InitialRecord(
    [property: JsonRequired] IReadOnlyList<JsonElement> Events,
    [property: JsonRequired] long RngWords,
    [property: JsonRequired] string StateDigest);

/// <summary>A replayable group of one root operation and its dependent answers.</summary>
public sealed record JournalUnit(
    [property: JsonRequired] string Role,
    [property: JsonRequired] string Status,
    [property: JsonRequired] int InitiatingSeat,
    [property: JsonRequired] int ActiveSeat,
    [property: JsonRequired] int Round,
    [property: JsonRequired] string Phase,
    [property: JsonRequired] IReadOnlyList<JournalStep> Decisions);

/// <summary>Schema 1's complete, capability-free deterministic session authority.</summary>
public sealed record SessionSave(
    [property: JsonRequired] string Format,
    [property: JsonRequired] int Schema,
    [property: JsonRequired] SessionCompatibility Compatibility,
    [property: JsonRequired] SessionIdentity Session,
    [property: JsonRequired] SessionSetup Setup,
    [property: JsonRequired] InitialRecord Initial,
    [property: JsonRequired] long Revision,
    [property: JsonRequired] int Cursor,
    [property: JsonRequired] int EditFrontier,
    [property: JsonRequired] PromptRecord? CurrentPrompt,
    [property: JsonRequired] IReadOnlyList<JournalUnit> Units)
{
    /// <summary>The required schema family marker.</summary>
    public const string FormatName = "marvel-session";

    /// <summary>The only schema this runtime reads and writes.</summary>
    public const int CurrentSchema = 1;

    /// <summary>Creates the zero-decision authority for a freshly dealt game.</summary>
    public static SessionSave Open(
        SessionCompatibility compatibility,
        string storageId,
        string label,
        SessionSetup setup,
        Game game,
        IReadOnlyList<GameEvent> setupEvents) =>
        new(
            FormatName,
            CurrentSchema,
            compatibility,
            new SessionIdentity(storageId, label, "active"),
            setup,
            new InitialRecord(
                [.. setupEvents.Select(JournalJson.Event)],
                game.State.Random.Generator.WordsConsumed,
                game.State.Digest().Canonical()),
            Revision: 0,
            Cursor: 0,
            EditFrontier: 0,
            game.Pending is null ? null : PromptRecord.From(game.Pending),
            []);
}

/// <summary>Strict, deterministic JSON for the canonical save document.</summary>
public static class SessionSaveJson
{
    /// <summary>The strict snake-case serialization contract for schema 1.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>Validates and writes one canonical save document.</summary>
    public static string Write(SessionSave save)
    {
        Validate(save);
        return JsonSerializer.Serialize(save, Options);
    }

    /// <summary>Strictly parses and validates one canonical save document.</summary>
    public static SessionSave Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            var save = JsonSerializer.Deserialize<SessionSave>(json, Options)
                ?? throw new SessionSaveException("save contains no session document");
            Validate(save);
            return save;
        }
        catch (JsonException failure)
        {
            throw new SessionSaveException("save is not valid schema JSON", failure);
        }
    }

    /// <summary>Rejects unsupported identities and structurally invalid history.</summary>
    public static void Validate(SessionSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (!string.Equals(save.Format, SessionSave.FormatName, StringComparison.Ordinal))
        {
            throw new SessionSaveException("save format is not supported");
        }

        if (save.Schema != SessionSave.CurrentSchema)
        {
            throw new SessionSaveException($"save schema {save.Schema} is not supported");
        }

        if (save.Compatibility is null || save.Session is null || save.Setup is null
            || save.Initial is null || save.Units is null)
        {
            throw new SessionSaveException("save is missing a required record");
        }

        if (string.IsNullOrWhiteSpace(save.Compatibility.Application)
            || string.IsNullOrWhiteSpace(save.Compatibility.ReplayContract)
            || string.IsNullOrWhiteSpace(save.Compatibility.RngContract)
            || string.IsNullOrWhiteSpace(save.Compatibility.StateDigest)
            || !Sha256(save.Compatibility.CardsSha256)
            || !Sha256(save.Compatibility.SetupSha256)
            || !Sha256(save.Compatibility.AbilitiesSha256))
        {
            throw new SessionSaveException("save compatibility identity is invalid");
        }

        if (save.Revision < 0 || save.Cursor < 0 || save.Cursor > save.Units.Count
            || save.EditFrontier < 0 || save.EditFrontier > save.Cursor)
        {
            throw new SessionSaveException("save history bounds are invalid");
        }

        if (save.Setup.Heroes is not { Count: > 0 }
            || save.Setup.Heroes.Any(string.IsNullOrWhiteSpace)
            || string.IsNullOrWhiteSpace(save.Setup.Scenario))
        {
            throw new SessionSaveException("save setup is invalid");
        }

        if (!StorageId(save.Session.StorageId)
            || string.IsNullOrWhiteSpace(save.Session.Label)
            || save.Session.Label.Length > 256
            || save.Session.Lifecycle is not ("active" or "retired"))
        {
            throw new SessionSaveException("save session identity is invalid");
        }

        if (save.Initial.Events is null
            || save.Initial.RngWords < 0
            || string.IsNullOrEmpty(save.Initial.StateDigest)
            || save.Units.Any(unit => unit is null
                || unit.Decisions is not { Count: > 0 }
                || unit.Status is not ("open" or "complete")
                || unit.Decisions.Any(step => step is null
                    || step.Prompt is null
                    || step.Decision is null
                    || step.Events is null
                    || step.RngWords < 0
                    || string.IsNullOrEmpty(step.StateFingerprint)
                    || step.Result is { Outcome: null or "" }
                    || step.Result is { Round: < 0 })))
        {
            throw new SessionSaveException("save replay records are invalid");
        }


        int open = -1;
        for (int index = 0; index < save.Units.Count; index++)
        {
            JournalUnit unit = save.Units[index];
            if (unit.Status == "open")
            {
                if (open >= 0 || index != save.Cursor - 1)
                {
                    throw new SessionSaveException("save has an invalid open history unit");
                }

                open = index;
            }
        }
    }

    private static bool Sha256(string? value) =>
        value is { Length: 64 } && value.All(character =>
            character is (>= '0' and <= '9') or (>= 'a' and <= 'f'));

    private static bool StorageId(string? value) =>
        value is { Length: 32 } && value.All(character =>
            character is (>= '0' and <= '9') or (>= 'a' and <= 'f'));

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new ResourceAllocationJsonConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}

/// <summary>The game and setup events freshly produced by a replay factory.</summary>
public sealed record ReplayOpenedGame(Game Game, IReadOnlyList<GameEvent> SetupEvents);

/// <summary>Reconstructs and verifies a save without mutating a live game.</summary>
public static class SessionReplay
{
    /// <summary>Deals and verifies the complete active prefix of a save.</summary>
    public static Game Verify(
        SessionSave save,
        SessionCompatibility expected,
        Func<SessionSetup, ReplayOpenedGame> open)
    {
        SessionSaveJson.Validate(save);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(open);
        RequireCompatibility(expected, save.Compatibility);

        Game complete = Replay(save, save.Units.Count, open);
        Game active = save.Cursor == save.Units.Count
            ? complete
            : Replay(save, save.Cursor, open);
        RequireCurrentPrompt(save.CurrentPrompt, active.Pending);
        return active;
    }

    private static Game Replay(
        SessionSave save,
        int unitCount,
        Func<SessionSetup, ReplayOpenedGame> open)
    {
        ReplayOpenedGame opened = open(save.Setup);
        Game game = opened.Game;
        JournalReplay.RequireEvents(save.Initial.Events, opened.SetupEvents, "initial events");
        JournalReplay.RequireRng(
            save.Initial.RngWords,
            game.State.Random.Generator.WordsConsumed,
            "initial RNG");
        JournalReplay.RequireFingerprint(
            save.Initial.StateDigest,
            game.State.Digest().Canonical(),
            "initial state");

        for (int unitIndex = 0; unitIndex < unitCount; unitIndex++)
        {
            JournalUnit unit = save.Units[unitIndex];
            if (unit.Decisions is null or { Count: 0 }
                || unit.ActiveSeat != game.Active
                || unit.Round != game.Round
                || !string.Equals(unit.Phase, game.Phase.ToString(), StringComparison.Ordinal))
            {
                throw new ReplayDivergenceException(
                    $"unit {unitIndex} engine position diverged");
            }

            int decisionIndex = 0;
            foreach (JournalStep step in unit.Decisions)
            {
                Prompt prompt = game.Pending ?? throw new ReplayDivergenceException(
                    $"unit {unitIndex} decision {decisionIndex} has no prompt");
                string context = $"unit {unitIndex} decision {decisionIndex}";
                JournalReplay.RequirePrompt(step.Prompt, prompt, $"{context} prompt");
                Decision decision = step.Decision.Resolve(prompt);
                var resolved = game.Resolve(decision);
                JournalReplay.RequireEvents(step.Events, resolved.Events, $"{context} events");
                JournalReplay.RequireRng(
                    step.RngWords,
                    game.State.Random.Generator.WordsConsumed,
                    $"{context} RNG");
                JournalReplay.RequireFingerprint(
                    step.StateFingerprint, Fingerprint(game), $"{context} state");
                if (!Equals(step.Result, Result(game)))
                {
                    throw new ReplayDivergenceException($"{context} result diverged");
                }
                decisionIndex++;
            }
        }

        return game;
    }

    /// <summary>Captures hidden state together with its terminal meaning.</summary>
    public static string Fingerprint(Game game) =>
        game.State.Digest().Fingerprint();

    /// <summary>Captures terminal outcome and round, or no result before game end.</summary>
    public static EngineResultRecord? Result(Game game) =>
        game.State.IsOver
            ? new EngineResultRecord(game.State.Result.ToString(), game.Round)
            : null;

    private static void RequireCurrentPrompt(PromptRecord? expected, Prompt? actual)
    {
        if (expected is null)
        {
            if (actual is not null)
            {
                throw new ReplayDivergenceException("current prompt diverged: expected none");
            }

            return;
        }

        if (actual is null)
        {
            throw new ReplayDivergenceException("current prompt diverged: expected a prompt");
        }

        JournalReplay.RequirePrompt(expected, actual, "current prompt");
    }

    private static void RequireCompatibility(
        SessionCompatibility expected, SessionCompatibility actual)
    {
        if (actual is null
            || !string.Equals(expected.ReplayContract, actual.ReplayContract, StringComparison.Ordinal)
            || !string.Equals(expected.RngContract, actual.RngContract, StringComparison.Ordinal)
            || !string.Equals(expected.StateDigest, actual.StateDigest, StringComparison.Ordinal)
            || !string.Equals(expected.CardsSha256, actual.CardsSha256, StringComparison.Ordinal)
            || !string.Equals(expected.SetupSha256, actual.SetupSha256, StringComparison.Ordinal)
            || !string.Equals(expected.AbilitiesSha256, actual.AbilitiesSha256, StringComparison.Ordinal))
        {
            throw new SessionSaveException("save compatibility does not match this engine and dataset");
        }
    }
}

/// <summary>A save cannot be safely parsed or replayed by this runtime.</summary>
public sealed class SessionSaveException(string message, Exception? inner = null)
    : Exception(message, inner);
