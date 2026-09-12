using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

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
    [property: JsonRequired] IReadOnlyList<JournalStep> Decisions,
    [property: JsonRequired] IReadOnlyList<InformationExposure> Exposures);

/// <summary>Schema 3's complete, capability-free deterministic session authority.</summary>
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

    /// <summary>The schema this runtime writes; schema 2 is read only for migration.</summary>
    public const int CurrentSchema = 3;

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
public static partial class SessionSaveJson
{
    /// <summary>The strict snake-case serialization contract for schema 3.</summary>
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
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("schema", out JsonElement schema)
                && schema.ValueKind == JsonValueKind.Number
                && schema.GetInt32() == 2)
            {
                return ReadSchemaTwo(json);
            }

            var save = JsonSerializer.Deserialize<SessionSave>(json, Options)
                ?? throw new SessionSaveException("save contains no session document");
            Validate(save);
            return save;
        }
        catch (Exception failure) when (failure is JsonException
            or FormatException
            or InvalidOperationException
            or NotSupportedException)
        {
            throw new SessionSaveException("save is not valid schema JSON", failure);
        }
    }

    /// <summary>Validates either the current schema or the one migratable predecessor.</summary>
    public static void ValidateReadable(SessionSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Schema == 2)
        {
            ValidateSchemaTwo(save);
            return;
        }

        Validate(save);
    }

    /// <summary>Rejects unsupported identities and structurally invalid history.</summary>
    public static void Validate(SessionSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        RequireSupportedEnvelope(save);
        RequireRecords(save);
        RequireCompatibilityIdentity(save.Compatibility);
        RequireHistoryBounds(save);
        RequireSetup(save.Setup);
        RequireSessionIdentity(save.Session);
        RequireReplayRecords(save);
        RequireHistoryShape(save);
    }

    private static bool Sha256(string? value) =>
        value is { Length: 64 } && value.All(character =>
            character is (>= '0' and <= '9') or (>= 'a' and <= 'f'));

    private static bool StorageId(string? value) =>
        value is { Length: 32 } && value.All(character =>
            character is (>= '0' and <= '9') or (>= 'a' and <= 'f'));

    private static SessionSave ReadSchemaTwo(string json)
    {
        var legacy = JsonSerializer.Deserialize<SchemaTwoSessionSave>(json, Options)
            ?? throw new SessionSaveException("save contains no session document");
        if (legacy.Units is null)
        {
            throw new JsonException("schema 2 units are null");
        }

        var save = new SessionSave(
            legacy.Format,
            legacy.Schema,
            legacy.Compatibility,
            legacy.Session,
            legacy.Setup,
            legacy.Initial,
            legacy.Revision,
            legacy.Cursor,
            legacy.EditFrontier,
            ReadSchemaTwoPrompt(legacy.CurrentPrompt),
            [.. legacy.Units.Select(unit => ConvertSchemaTwoUnit(
                unit ?? throw new JsonException("schema 2 unit is null")))]);
        ValidateSchemaTwo(save);
        return save;
    }

    private static JournalUnit ConvertSchemaTwoUnit(SchemaTwoJournalUnit unit)
    {
        if (unit.Decisions is null)
        {
            throw new JsonException("schema 2 unit decisions are null");
        }

        return new JournalUnit(
            unit.Role,
            unit.Status,
            unit.InitiatingSeat,
            unit.ActiveSeat,
            unit.Round,
            unit.Phase,
            [.. unit.Decisions.Select(step => ConvertSchemaTwoStep(
                step ?? throw new JsonException("schema 2 step is null")))],
            unit.Exposures);
    }

    private static JournalStep ConvertSchemaTwoStep(SchemaTwoJournalStep step) =>
        new(
            SchemaTwoPromptJson.Read(step.Prompt),
            step.Decision,
            step.Events,
            step.RngWords,
            step.StateFingerprint,
            step.Result);

    private static PromptRecord? ReadSchemaTwoPrompt(JsonElement? prompt) =>
        prompt is null || prompt.Value.ValueKind == JsonValueKind.Null
            ? null
            : SchemaTwoPromptJson.Read(prompt.Value);

    private static void ValidateSchemaTwo(SessionSave save)
    {
        if (save.Schema != 2)
        {
            throw new SessionSaveException("schema 2 save is not migratable");
        }

        Validate(save with { Schema = SessionSave.CurrentSchema });
    }

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

/// <summary>The frozen session envelope written by schema 2.</summary>
internal sealed record SchemaTwoSessionSave(
    [property: JsonRequired] string Format,
    [property: JsonRequired] int Schema,
    [property: JsonRequired] SessionCompatibility Compatibility,
    [property: JsonRequired] SessionIdentity Session,
    [property: JsonRequired] SessionSetup Setup,
    [property: JsonRequired] InitialRecord Initial,
    [property: JsonRequired] long Revision,
    [property: JsonRequired] int Cursor,
    [property: JsonRequired] int EditFrontier,
    [property: JsonRequired] JsonElement? CurrentPrompt,
    [property: JsonRequired] IReadOnlyList<SchemaTwoJournalUnit> Units);

/// <summary>One frozen schema 2 history unit.</summary>
internal sealed record SchemaTwoJournalUnit(
    [property: JsonRequired] string Role,
    [property: JsonRequired] string Status,
    [property: JsonRequired] int InitiatingSeat,
    [property: JsonRequired] int ActiveSeat,
    [property: JsonRequired] int Round,
    [property: JsonRequired] string Phase,
    [property: JsonRequired] IReadOnlyList<SchemaTwoJournalStep> Decisions,
    [property: JsonRequired] IReadOnlyList<InformationExposure> Exposures);

/// <summary>One frozen schema 2 decision and its derived replay facts.</summary>
internal sealed record SchemaTwoJournalStep(
    [property: JsonRequired] JsonElement Prompt,
    [property: JsonRequired] DurableDecision Decision,
    [property: JsonRequired] IReadOnlyList<JsonElement> Events,
    [property: JsonRequired] long RngWords,
    [property: JsonRequired] string StateFingerprint,
    [property: JsonRequired] EngineResultRecord? Result);

/// <summary>The game and setup events freshly produced by a replay factory.</summary>
public sealed record ReplayOpenedGame(Game Game, IReadOnlyList<GameEvent> SetupEvents);

/// <summary>A newly derived active trace and the game it produces.</summary>
public sealed record RewrittenTrace(
    Game Game,
    IReadOnlyList<JournalUnit> Units,
    int EditFrontier);

/// <summary>
/// Engine-authored facts needed to describe one active, completed history unit.
/// This is neither save data nor a client wire type.
/// </summary>
public sealed record HistoryUnitInspection(
    int Cursor,
    int Actor,
    string ActorName,
    string Role,
    string Phase,
    string? Verb,
    string Action,
    int? Subject,
    IReadOnlyList<int> ResourceGeneratorIds,
    IReadOnlyList<string> ResourceGenerators,
    IReadOnlyList<GameEvent> Events,
    string? Outcome);

/// <summary>Reconstructs and verifies a save without mutating a live game.</summary>
public static partial class SessionReplay
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

        ReplayResult complete = Replay(
            save, save.Units.Count, open, requireExposures: true);
        Game active = save.Cursor == save.Units.Count
            ? complete.Game
            : Replay(save, save.Cursor, open, requireExposures: true).Game;
        RequireCurrentPrompt(save.CurrentPrompt, active.Pending);
        return active;
    }

    /// <summary>
    /// Verifies the canonical save and reconstructs one retained unit boundary.
    /// </summary>
    /// <remarks>
    /// History editing is a product operation. It replays from setup instead of
    /// reversing rules mutations in place.
    /// </remarks>
    public static Game VerifyAtCursor(
        SessionSave save,
        SessionCompatibility expected,
        Func<SessionSetup, ReplayOpenedGame> open,
        int cursor)
    {
        Game current = Verify(save, expected, open);
        if (cursor < 0 || cursor > save.Units.Count)
        {
            throw new SessionSaveException("history cursor is outside the retained trace");
        }

        return cursor == save.Cursor
            ? current
            : Replay(save, cursor, open, requireExposures: true).Game;
    }

    /// <summary>
    /// Replays the active prefix once and returns the engine facts needed for
    /// visibility-safe history presentation. Open units remain replayed but
    /// are not presented as completed actions.
    /// </summary>
    public static IReadOnlyList<HistoryUnitInspection> InspectActiveHistory(
        SessionSave save,
        SessionCompatibility expected,
        Func<SessionSetup, ReplayOpenedGame> open)
    {
        SessionSaveJson.Validate(save);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(open);
        RequireCompatibility(expected, save.Compatibility);
        var history = new List<HistoryUnitInspection>(save.Cursor);
        _ = Replay(
            save,
            save.Cursor,
            open,
            requireExposures: true,
            history);
        return history;
    }

    /// <summary>
    /// Rebuilds a complete active trace from durable inputs in a proposed order.
    /// </summary>
    /// <remarks>
    /// Reordering is a product operation. Derived prompts, events, RNG counts,
    /// fingerprints, results, positions and information signals are generated
    /// again; none are copied from the prior order.
    /// </remarks>
    public static RewrittenTrace Rewrite(
        SessionSave save,
        SessionCompatibility expected,
        Func<SessionSetup, ReplayOpenedGame> open,
        IReadOnlyList<int> sourceOrder)
    {
        _ = Verify(save, expected, open);
        ArgumentNullException.ThrowIfNull(sourceOrder);
        RequireRewriteOrder(save, sourceOrder);

        Game game = Replay(save, 0, open, requireExposures: true).Game;
        var rewritten = new List<JournalUnit>(sourceOrder.Count);
        int frontier = 0;
        foreach (int sourceIndex in sourceOrder)
        {
            JournalUnit unit = RewriteUnit(save.Units[sourceIndex], sourceIndex, game);
            rewritten.Add(unit);
            if (unit.Exposures.Count > 0)
            {
                frontier = rewritten.Count;
            }
        }

        return new RewrittenTrace(game, rewritten, frontier);
    }

    private static void RequireRewriteOrder(
        SessionSave save,
        IReadOnlyList<int> sourceOrder)
    {
        if (sourceOrder.Count != save.Cursor
            || !sourceOrder.Order().SequenceEqual(Enumerable.Range(0, save.Cursor)))
        {
            throw new SessionSaveException(
                "rewrite order is not a permutation of the active trace");
        }
    }

    private static JournalUnit RewriteUnit(JournalUnit source, int sourceIndex, Game game)
    {
        if (source.Status != "complete")
        {
            throw new ReplayDivergenceException(
                $"unit {sourceIndex} is not complete for rewriting");
        }

        int active = game.Active;
        int round = game.Round;
        string phase = game.Phase.ToString();
        string? role = null;
        var steps = new List<JournalStep>(source.Decisions.Count);
        IReadOnlyList<InformationExposure> exposures = [];
        for (int decisionIndex = 0; decisionIndex < source.Decisions.Count; decisionIndex++)
        {
            RequireRewriteContinuation(game, sourceIndex, decisionIndex);
            RewrittenDecision rewritten = RewriteDecision(
                source.Decisions[decisionIndex], sourceIndex, decisionIndex, game, exposures,
                deriveRole: role is null);
            role ??= rewritten.Role;
            exposures = rewritten.Exposures;
            steps.Add(rewritten.Step);
        }

        RequireRewriteBoundary(game, sourceIndex);
        return new JournalUnit(
            RewrittenRole(game, role, sourceIndex),
            "complete",
            source.Decisions[0].Decision.Actor,
            active,
            round,
            phase,
            steps,
            exposures);
    }

    private static void RequireRewriteContinuation(Game game, int unitIndex, int decisionIndex)
    {
        if (decisionIndex > 0 && (game.Pending is null || game.IsRootPrompt))
        {
            throw new ReplayDivergenceException(
                $"unit {unitIndex} reached a boundary before its dependent decisions ended");
        }
    }

    private static RewrittenDecision RewriteDecision(
        JournalStep input,
        int unitIndex,
        int decisionIndex,
        Game game,
        IReadOnlyList<InformationExposure> exposures,
        bool deriveRole)
    {
        Prompt prompt = game.Pending ?? throw new ReplayDivergenceException(
            $"unit {unitIndex} decision {decisionIndex} has no prompt");
        Decision decision = input.Decision.Resolve(prompt);
        string? role = deriveRole ? UnitRole(game, prompt, decision) : null;
        long rngBefore = game.State.Random.Generator.WordsConsumed;
        var resolved = game.Resolve(decision);
        IReadOnlyList<InformationExposure> rewrittenExposures = InformationFrontier.Merge(
            exposures,
            InformationFrontier.Classify(
                game.State.Players,
                rngBefore,
                game.State.Random.Generator.WordsConsumed,
                resolved.Information,
                resolved.Events,
                game.Pending));
        JournalStep step = JournalStep.From(
            input.Decision.Actor,
            prompt,
            decision,
            resolved.Events,
            game.State.Random.Generator.WordsConsumed,
            Fingerprint(game),
            Result(game));
        return new RewrittenDecision(role, step, rewrittenExposures);
    }

    private static void RequireRewriteBoundary(Game game, int unitIndex)
    {
        if (game.Pending is not null && !game.IsRootPrompt)
        {
            throw new ReplayDivergenceException(
                $"unit {unitIndex} did not reach its complete boundary");
        }
    }

    private static string RewrittenRole(Game game, string? role, int unitIndex) =>
        game.Pending is null
            ? "terminal"
            : role ?? throw new ReplayDivergenceException(
                $"unit {unitIndex} has no root decision");

    /// <summary>
    /// Replays the strict predecessor format before producing schema 3.
    /// </summary>
    public static SessionSave MigrateSchemaTwo(
        SessionSave save,
        SessionCompatibility expected,
        Func<SessionSetup, ReplayOpenedGame> open)
    {
        SessionSaveJson.ValidateReadable(save);
        if (save.Schema != 2)
        {
            throw new SessionSaveException("only schema 2 can be migrated");
        }

        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(open);
        RequireCompatibility(expected, save.Compatibility);
        ReplayResult replayed = Replay(save, save.Units.Count, open, requireExposures: true);
        RequireCurrentPrompt(save.CurrentPrompt, replayed.Game.Pending);
        SessionSave migrated = save with
        {
            Schema = SessionSave.CurrentSchema,
            Compatibility = expected,
            Units = [.. save.Units.Select(unit => unit with
            {
                Decisions = [.. unit.Decisions],
                Exposures = [.. unit.Exposures],
            })],
        };
        SessionSaveJson.Validate(migrated);
        return migrated;
    }

    private static ReplayResult Replay(
        SessionSave save,
        int unitCount,
        Func<SessionSetup, ReplayOpenedGame> open,
        bool requireExposures,
        List<HistoryUnitInspection>? history = null)
    {
        ReplayOpenedGame opened = open(save.Setup);
        Game game = opened.Game;
        RequireInitialReplay(save.Initial, opened, game);
        var derived = new List<IReadOnlyList<InformationExposure>>(unitCount);
        for (int unitIndex = 0; unitIndex < unitCount; unitIndex++)
        {
            derived.Add(ReplayUnit(
                save.Units[unitIndex], unitIndex, game, requireExposures, history));
        }

        RequireReplayFrontier(save, unitCount, requireExposures);
        return new ReplayResult(game, derived);
    }

    private static void RequireInitialReplay(
        InitialRecord initial,
        ReplayOpenedGame opened,
        Game game)
    {
        JournalReplay.RequireEvents(initial.Events, opened.SetupEvents, "initial events");
        JournalReplay.RequireRng(
            initial.RngWords,
            game.State.Random.Generator.WordsConsumed,
            "initial RNG");
        JournalReplay.RequireFingerprint(
            initial.StateDigest,
            game.State.Digest().Canonical(),
            "initial state");
    }

    private static IReadOnlyList<InformationExposure> ReplayUnit(
        JournalUnit unit,
        int unitIndex,
        Game game,
        bool requireExposures,
        List<HistoryUnitInspection>? history)
    {
        RequireUnitPosition(unit, unitIndex, game);
        ReplayHistory? inspection = CreateReplayHistory(history, unit);
        IReadOnlyList<InformationExposure> exposures = [];
        string? derivedRole = null;
        for (int decisionIndex = 0; decisionIndex < unit.Decisions.Count; decisionIndex++)
        {
            ReplayUnitDecision(
                unit,
                unitIndex,
                decisionIndex,
                game,
                inspection,
                ref derivedRole,
                ref exposures);
        }

        RequireUnitBoundary(unit, unitIndex, game, derivedRole);
        AppendReplayHistory(inspection, history, unit, unitIndex);
        RequireReplayExposures(requireExposures, unit, unitIndex, exposures);
        return exposures;
    }

    private static ReplayHistory? CreateReplayHistory(
        List<HistoryUnitInspection>? history,
        JournalUnit unit) =>
        history is not null && unit.Status == "complete" ? new ReplayHistory() : null;

    private static void ReplayUnitDecision(
        JournalUnit unit,
        int unitIndex,
        int decisionIndex,
        Game game,
        ReplayHistory? inspection,
        ref string? derivedRole,
        ref IReadOnlyList<InformationExposure> exposures)
    {
        RequireReplayContinuation(game, unitIndex, decisionIndex);
        JournalStep step = unit.Decisions[decisionIndex];
        Prompt prompt = game.Pending ?? throw new ReplayDivergenceException(
            $"unit {unitIndex} decision {decisionIndex} has no prompt");
        string context = $"unit {unitIndex} decision {decisionIndex}";
        JournalReplay.RequirePrompt(step.Prompt, prompt, $"{context} prompt");
        Decision decision = step.Decision.Resolve(prompt);
        ObserveReplayRoot(inspection, unit, step, decisionIndex, game, prompt, decision);
        ObserveReplayResources(inspection, step, decision, game);
        DeriveReplayRole(unit, step, unitIndex, decisionIndex, game, prompt, decision,
            ref derivedRole);
        ReplayDecision resolved = ResolveReplayDecision(
            step, decision, context, game, exposures);
        exposures = resolved.Exposures;
        ObserveReplayEvents(inspection, resolved.Events);
    }

    private static void ObserveReplayRoot(
        ReplayHistory? inspection,
        JournalUnit unit,
        JournalStep step,
        int decisionIndex,
        Game game,
        Prompt prompt,
        Decision decision)
    {
        if (decisionIndex == 0)
        {
            inspection?.ObserveRoot(unit, step, game, prompt, decision);
        }
    }

    private static void DeriveReplayRole(
        JournalUnit unit,
        JournalStep step,
        int unitIndex,
        int decisionIndex,
        Game game,
        Prompt prompt,
        Decision decision,
        ref string? derivedRole)
    {
        if (decisionIndex != 0)
        {
            return;
        }

        RequireRootMetadata(unit, step, unitIndex);
        derivedRole = UnitRole(game, prompt, decision);
    }

    private static void ObserveReplayResources(
        ReplayHistory? inspection,
        JournalStep step,
        Decision decision,
        Game game) =>
        inspection?.ObserveResources(step, decision, game);

    private static void ObserveReplayEvents(
        ReplayHistory? inspection,
        IReadOnlyList<GameEvent> events) =>
        inspection?.ObserveEvents(events);

    private static void AppendReplayHistory(
        ReplayHistory? inspection,
        List<HistoryUnitInspection>? history,
        JournalUnit unit,
        int unitIndex)
    {
        if (inspection is not null)
        {
            inspection.Append(history!, unit, unitIndex);
        }
    }

    private static void RequireReplayExposures(
        bool requireExposures,
        JournalUnit unit,
        int unitIndex,
        IReadOnlyList<InformationExposure> exposures)
    {
        if (requireExposures)
        {
            RequireExposures(unit.Exposures, exposures, $"unit {unitIndex} exposure");
        }
    }

    private static void RequireUnitPosition(JournalUnit unit, int unitIndex, Game game)
    {
        if (unit.Decisions is null or { Count: 0 }
            || unit.ActiveSeat != game.Active
            || unit.Round != game.Round
            || !string.Equals(unit.Phase, game.Phase.ToString(), StringComparison.Ordinal))
        {
            throw new ReplayDivergenceException(
                $"unit {unitIndex} engine position diverged");
        }
    }

    private static void RequireReplayContinuation(Game game, int unitIndex, int decisionIndex)
    {
        if (decisionIndex > 0 && (game.Pending is null || game.IsRootPrompt))
        {
            throw new ReplayDivergenceException($"unit {unitIndex} crossed a root boundary");
        }
    }

    private static void RequireRootMetadata(JournalUnit unit, JournalStep step, int unitIndex)
    {
        if (unit.InitiatingSeat != step.Decision.Actor)
        {
            throw new ReplayDivergenceException(
                $"unit {unitIndex} root metadata diverged");
        }
    }

    private static ReplayDecision ResolveReplayDecision(
        JournalStep step,
        Decision decision,
        string context,
        Game game,
        IReadOnlyList<InformationExposure> exposures)
    {
        long rngBefore = game.State.Random.Generator.WordsConsumed;
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

        IReadOnlyList<InformationExposure> merged = InformationFrontier.Merge(
            exposures,
            InformationFrontier.Classify(
                game.State.Players,
                rngBefore,
                game.State.Random.Generator.WordsConsumed,
                resolved.Information,
                resolved.Events,
                game.Pending));
        return new ReplayDecision(resolved.Events, merged);
    }

    private static void RequireUnitBoundary(
        JournalUnit unit,
        int unitIndex,
        Game game,
        string? derivedRole)
    {
        bool reachedBoundary = game.Pending is null || game.IsRootPrompt;
        if ((unit.Status == "complete") != reachedBoundary)
        {
            throw new ReplayDivergenceException(
                $"unit {unitIndex} completion status diverged");
        }

        string expectedRole = ReplayedRole(game, derivedRole, unitIndex);
        if (!string.Equals(unit.Role, expectedRole, StringComparison.Ordinal))
        {
            throw new ReplayDivergenceException($"unit {unitIndex} role diverged");
        }
    }

    private static string ReplayedRole(Game game, string? role, int unitIndex) =>
        game.Pending is null
            ? "terminal"
            : role ?? throw new ReplayDivergenceException(
                $"unit {unitIndex} has no root decision");

    private static void RequireReplayFrontier(
        SessionSave save,
        int unitCount,
        bool requireExposures)
    {
        if (!requireExposures || unitCount != save.Units.Count)
        {
            return;
        }

        int frontier = save.Units
            .Select((unit, index) => unit.Exposures.Count > 0 ? index + 1 : 0)
            .DefaultIfEmpty(0)
            .Max();
        if (frontier != save.EditFrontier)
        {
            throw new ReplayDivergenceException("information frontier diverged");
        }
    }

    /// <summary>Derives the history role of a root decision from engine truth.</summary>
    public static string UnitRole(Game game, Prompt prompt, Decision decision)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(decision);
        if (game.IsForcedResolutionPrompt)
        {
            return "forced_resolution";
        }

        if (game.Phase != GamePhase.PlayerTurn)
        {
            return "phase_step";
        }

        string? verb = decision.IsDecline
            ? null
            : prompt.Affordances.Single(option => option.Id == decision.Affordance).Verb;
        return string.Equals(verb, Game.ChangeForm, StringComparison.Ordinal)
            || string.Equals(verb, Game.EndPhaseVerb, StringComparison.Ordinal)
            || decision.IsDecline
                ? "turn_control"
                : "turn_action";
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

    private static void RequireExposures(
        IReadOnlyList<InformationExposure> expected,
        IReadOnlyList<InformationExposure> actual,
        string context)
    {
        string expectedJson = JsonSerializer.Serialize(expected, SessionSaveJson.Options);
        string actualJson = JsonSerializer.Serialize(actual, SessionSaveJson.Options);
        if (!string.Equals(expectedJson, actualJson, StringComparison.Ordinal))
        {
            throw new ReplayDivergenceException($"{context} diverged");
        }
    }

    private static void RequireCompatibility(
        SessionCompatibility expected, SessionCompatibility actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        if (actual is null)
        {
            throw new SessionSaveException("save compatibility does not match this engine and dataset");
        }

        if (!string.Equals(expected.Application, actual.Application, StringComparison.Ordinal)
            && ApplicationVersion.Parse(actual.Application)
                .CompareTo(ApplicationVersion.Parse(expected.Application)) > 0)
        {
            throw new SessionCompatibilityException(
                "unsupported_downgrade",
                "save application version is newer than this runtime");
        }

        RequireIdentity(expected.ReplayContract, actual.ReplayContract,
            "replay_identity_mismatch");
        RequireIdentity(expected.RngContract, actual.RngContract,
            "rng_identity_mismatch");
        RequireIdentity(expected.StateDigest, actual.StateDigest,
            "digest_identity_mismatch");
        RequireIdentity(expected.CardsSha256, actual.CardsSha256,
            "cards_dataset_mismatch");
        RequireIdentity(expected.SetupSha256, actual.SetupSha256,
            "setup_dataset_mismatch");
        RequireIdentity(expected.AbilitiesSha256, actual.AbilitiesSha256,
            "abilities_dataset_mismatch");
    }

    private static void RequireIdentity(string expected, string actual, string category)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new SessionCompatibilityException(
                category,
                $"save compatibility differs for {category}");
        }
    }

    private sealed record ReplayResult(
        Game Game,
        IReadOnlyList<IReadOnlyList<InformationExposure>> Exposures);

    private sealed record ReplayDecision(
        IReadOnlyList<GameEvent> Events,
        IReadOnlyList<InformationExposure> Exposures);

    private sealed record RewrittenDecision(
        string? Role,
        JournalStep Step,
        IReadOnlyList<InformationExposure> Exposures);

}

/// <summary>A save cannot be safely parsed or replayed by this runtime.</summary>
public class SessionSaveException(string message, Exception? inner = null)
    : Exception(message, inner);

/// <summary>A bounded compatibility category for operator quarantine diagnostics.</summary>
public sealed class SessionCompatibilityException(string category, string message)
    : SessionSaveException(message)
{
    /// <summary>The stable non-secret mismatch category.</summary>
    public string Category { get; } = category;
}
