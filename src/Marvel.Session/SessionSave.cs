using System.Text.Json;
using System.Text.Json.Nodes;
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

/// <summary>Schema 2's complete, capability-free deterministic session authority.</summary>
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

    /// <summary>The schema this runtime writes; schema 1 is read only for migration.</summary>
    public const int CurrentSchema = 2;

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
    /// <summary>The strict snake-case serialization contract for schema 2.</summary>
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
                && schema.GetInt32() == 1)
            {
                return ReadSchemaOne(json);
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
        if (save.Schema == 1)
        {
            ValidateSchemaOne(save);
            return;
        }

        Validate(save);
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
                || unit.Exposures is null
                || unit.Status is not ("open" or "complete")
                || unit.Decisions.Any(step => step is null
                    || step.Prompt is null
                    || step.Decision is null
                    || step.Events is null
                    || step.RngWords < 0
                    || string.IsNullOrEmpty(step.StateFingerprint)
                    || step.Result is { Outcome: null or "" }
                    || step.Result is { Round: < 0 })
                || unit.Exposures.Any(exposure =>
                    !InformationFrontier.IsCanonical(exposure, save.Setup.Heroes.Count))
                || unit.Exposures.Select(exposure => exposure.Reason)
                    .Distinct(StringComparer.Ordinal).Count() != unit.Exposures.Count))
        {
            throw new SessionSaveException("save replay records are invalid");
        }


        int open = -1;
        int recordedFrontier = 0;
        for (int index = 0; index < save.Units.Count; index++)
        {
            JournalUnit unit = save.Units[index];
            if (unit.Exposures.Count > 0)
            {
                recordedFrontier = index + 1;
            }

            if (unit.Status == "open")
            {
                if (open >= 0 || index != save.Cursor - 1)
                {
                    throw new SessionSaveException("save has an invalid open history unit");
                }

                open = index;
            }
        }

        if (recordedFrontier != save.EditFrontier)
        {
            throw new SessionSaveException("save information frontier is invalid");
        }
    }

    private static bool Sha256(string? value) =>
        value is { Length: 64 } && value.All(character =>
            character is (>= '0' and <= '9') or (>= 'a' and <= 'f'));

    private static bool StorageId(string? value) =>
        value is { Length: 32 } && value.All(character =>
            character is (>= '0' and <= '9') or (>= 'a' and <= 'f'));

    private static SessionSave ReadSchemaOne(string json)
    {
        JsonObject root = JsonNode.Parse(json) as JsonObject
            ?? throw new JsonException("schema 1 save is not an object");
        if (root["units"] is not JsonArray units)
        {
            throw new JsonException("schema 1 save has no units array");
        }

        foreach (JsonNode? node in units)
        {
            if (node is not JsonObject unit || unit.ContainsKey("exposures"))
            {
                throw new JsonException("schema 1 unit shape is invalid");
            }

            unit.Add("exposures", new JsonArray());
        }

        var save = JsonSerializer.Deserialize<SessionSave>(root.ToJsonString(), Options)
            ?? throw new SessionSaveException("save contains no session document");
        ValidateSchemaOne(save);
        return save;
    }

    private static void ValidateSchemaOne(SessionSave save)
    {
        if (save.Schema != 1)
        {
            throw new SessionSaveException("schema 1 save is not migratable");
        }

        // Validate the shared document shape before inspecting collections that
        // a malformed predecessor document could have omitted.
        Validate(save with { Schema = SessionSave.CurrentSchema });
        if (save.EditFrontier != 0
            || save.Cursor != save.Units.Count
            || save.Units.Any(unit => unit.Exposures.Count != 0))
        {
            throw new SessionSaveException("schema 1 save is not migratable");
        }

        // Schema 1 has the same shape except for per-unit exposure records. It
        // predates history editing, so only the complete active trace with the
        // original zero frontier is a state this runtime ever wrote.
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
        if (sourceOrder.Count != save.Cursor
            || !sourceOrder.Order().SequenceEqual(Enumerable.Range(0, save.Cursor)))
        {
            throw new SessionSaveException(
                "rewrite order is not a permutation of the active trace");
        }

        Game game = Replay(save, 0, open, requireExposures: true).Game;
        var rewritten = new List<JournalUnit>(sourceOrder.Count);
        int frontier = 0;
        foreach (int sourceIndex in sourceOrder)
        {
            JournalUnit source = save.Units[sourceIndex];
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
            for (int decisionIndex = 0;
                decisionIndex < source.Decisions.Count;
                decisionIndex++)
            {
                if (decisionIndex > 0 && (game.Pending is null || game.IsRootPrompt))
                {
                    throw new ReplayDivergenceException(
                        $"unit {sourceIndex} reached a boundary before its dependent decisions ended");
                }

                JournalStep input = source.Decisions[decisionIndex];
                Prompt prompt = game.Pending ?? throw new ReplayDivergenceException(
                    $"unit {sourceIndex} decision {decisionIndex} has no prompt");
                Decision decision = input.Decision.Resolve(prompt);
                role ??= UnitRole(game, prompt, decision);
                long rngBefore = game.State.Random.Generator.WordsConsumed;
                var resolved = game.Resolve(decision);
                exposures = InformationFrontier.Merge(
                    exposures,
                    InformationFrontier.Classify(
                        game.State.Players,
                        rngBefore,
                        game.State.Random.Generator.WordsConsumed,
                        resolved.Information,
                        resolved.Events,
                        game.Pending));
                steps.Add(JournalStep.From(
                    input.Decision.Actor,
                    prompt,
                    decision,
                    resolved.Events,
                    game.State.Random.Generator.WordsConsumed,
                    Fingerprint(game),
                    Result(game)));
            }

            if (game.Pending is not null && !game.IsRootPrompt)
            {
                throw new ReplayDivergenceException(
                    $"unit {sourceIndex} did not reach its complete boundary");
            }

            string rewrittenRole = game.Pending is null
                ? "terminal"
                : role ?? throw new ReplayDivergenceException(
                    $"unit {sourceIndex} has no root decision");
            rewritten.Add(new JournalUnit(
                rewrittenRole,
                "complete",
                source.Decisions[0].Decision.Actor,
                active,
                round,
                phase,
                steps,
                exposures));
            if (exposures.Count > 0)
            {
                frontier = rewritten.Count;
            }
        }

        return new RewrittenTrace(game, rewritten, frontier);
    }

    /// <summary>
    /// Replays the strict predecessor format and derives schema 2's knowledge records.
    /// </summary>
    public static SessionSave MigrateSchemaOne(
        SessionSave save,
        SessionCompatibility expected,
        Func<SessionSetup, ReplayOpenedGame> open)
    {
        SessionSaveJson.ValidateReadable(save);
        if (save.Schema != 1)
        {
            throw new SessionSaveException("only schema 1 can be migrated");
        }

        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(open);
        RequireCompatibility(expected, save.Compatibility);
        ReplayResult replayed = Replay(
            save, save.Units.Count, open, requireExposures: false);
        RequireCurrentPrompt(save.CurrentPrompt, replayed.Game.Pending);
        var units = save.Units.Select((unit, index) => unit with
        {
            Decisions = [.. unit.Decisions],
            Exposures = replayed.Exposures[index],
        }).ToList();
        int frontier = units
            .Select((unit, index) => unit.Exposures.Count > 0 ? index + 1 : 0)
            .DefaultIfEmpty(0)
            .Max();
        SessionSave migrated = save with
        {
            Schema = SessionSave.CurrentSchema,
            Compatibility = expected,
            EditFrontier = frontier,
            Units = units,
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
        var derived = new List<IReadOnlyList<InformationExposure>>(unitCount);
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
            IReadOnlyList<InformationExposure> exposures = [];
            if (unit.Decisions is null or { Count: 0 }
                || unit.ActiveSeat != game.Active
                || unit.Round != game.Round
                || !string.Equals(unit.Phase, game.Phase.ToString(), StringComparison.Ordinal))
            {
                throw new ReplayDivergenceException(
                    $"unit {unitIndex} engine position diverged");
            }

            int decisionIndex = 0;
            string? derivedRole = null;
            int? historyActor = null;
            string? historyActorName = null;
            string? historyVerb = null;
            string? historyAction = null;
            int? historySubject = null;
            var historyResources = new List<string>();
            var historyResourceIdsInOrder = new List<int>();
            var historyResourceIds = new HashSet<int>();
            var historyEvents = new List<GameEvent>();
            foreach (JournalStep step in unit.Decisions)
            {
                if (decisionIndex > 0 && (game.Pending is null || game.IsRootPrompt))
                {
                    throw new ReplayDivergenceException(
                        $"unit {unitIndex} crossed a root boundary");
                }

                Prompt prompt = game.Pending ?? throw new ReplayDivergenceException(
                    $"unit {unitIndex} decision {decisionIndex} has no prompt");
                string context = $"unit {unitIndex} decision {decisionIndex}";
                JournalReplay.RequirePrompt(step.Prompt, prompt, $"{context} prompt");
                Decision decision = step.Decision.Resolve(prompt);
                if (decisionIndex == 0
                    && history is not null
                    && unit.Status == "complete")
                {
                    Affordance? selected = decision.IsDecline
                        ? null
                        : prompt.Affordances.Single(option => option.Id == decision.Affordance);
                    Card? anchorCard = selected?.AnchorId is int anchor
                        && anchor >= 0
                        && anchor < game.State.Cards.Count
                            ? game.State.Cards[anchor]
                            : null;
                    string action = unit.Role == "phase_step"
                        ? selected?.Label ?? prompt.Label
                        : anchorCard is not null
                            ? game.State.Facts.Title(anchorCard.FaceId)
                            : selected?.Label ?? prompt.Label;
                    historyActor = step.Decision.Actor;
                    historyActorName = game.State.Seats[step.Decision.Actor].Name;
                    historyVerb = selected is not null
                        && string.Equals(selected.Verb, Game.ActionVerb, StringComparison.Ordinal)
                        && anchorCard is not null
                        && game.State.Facts.Kind(anchorCard.FaceId) == CardKind.Event
                            ? CardPlay.Verb
                            : decision.IsDecline && game.Phase == GamePhase.PlayerTurn
                                ? Game.EndPhaseVerb
                                : selected?.Verb;
                    historyAction = action;
                    historySubject = anchorCard?.ObjectId;
                }
                if (history is not null && unit.Status == "complete")
                {
                    foreach (int generator in decision.Spent.Where(
                                 historyResourceIds.Add))
                    {
                        historyResourceIdsInOrder.Add(generator);
                        historyResources.Add(
                            game.State.Abilities.ResourceGeneratorName(
                                game.State, step.Decision.Actor, generator));
                    }
                }
                if (decisionIndex == 0 && unit.InitiatingSeat != step.Decision.Actor)
                {
                    throw new ReplayDivergenceException(
                        $"unit {unitIndex} root metadata diverged");
                }
                derivedRole ??= UnitRole(game, prompt, decision);
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
                if (history is not null && unit.Status == "complete")
                {
                    historyEvents.AddRange(resolved.Events);
                }
                exposures = InformationFrontier.Merge(
                    exposures,
                    InformationFrontier.Classify(
                        game.State.Players,
                        rngBefore,
                        game.State.Random.Generator.WordsConsumed,
                        resolved.Information,
                        resolved.Events,
                        game.Pending));
                decisionIndex++;
            }

            bool reachedBoundary = game.Pending is null || game.IsRootPrompt;
            if ((unit.Status == "complete") != reachedBoundary)
            {
                throw new ReplayDivergenceException(
                    $"unit {unitIndex} completion status diverged");
            }

            string expectedRole = game.Pending is null
                ? "terminal"
                : derivedRole ?? throw new ReplayDivergenceException(
                    $"unit {unitIndex} has no root decision");
            if (!string.Equals(unit.Role, expectedRole, StringComparison.Ordinal))
            {
                throw new ReplayDivergenceException(
                    $"unit {unitIndex} role diverged");
            }

            if (history is not null && unit.Status == "complete")
            {
                history.Add(new HistoryUnitInspection(
                    unitIndex,
                    historyActor ?? throw new ReplayDivergenceException(
                        $"unit {unitIndex} has no history actor"),
                    historyActorName ?? throw new ReplayDivergenceException(
                        $"unit {unitIndex} has no history actor name"),
                    unit.Role,
                    unit.Phase,
                    historyVerb,
                    historyAction ?? throw new ReplayDivergenceException(
                        $"unit {unitIndex} has no history action"),
                    historySubject,
                    historyResourceIdsInOrder,
                    historyResources,
                    historyEvents,
                    unit.Decisions[^1].Result?.Outcome));
            }

            if (requireExposures)
            {
                RequireExposures(unit.Exposures, exposures, $"unit {unitIndex} exposure");
            }
            derived.Add(exposures);
        }

        if (requireExposures && unitCount == save.Units.Count)
        {
            int frontier = save.Units
                .Select((unit, index) => unit.Exposures.Count > 0 ? index + 1 : 0)
                .DefaultIfEmpty(0)
                .Max();
            if (frontier != save.EditFrontier)
            {
                throw new ReplayDivergenceException("information frontier diverged");
            }
        }

        return new ReplayResult(game, derived);
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
