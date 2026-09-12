using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>Replay identities selected by this engine build and its datasets.</summary>

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

        ReplayResult complete = SessionReplayVerification.Replay(
            save, save.Units.Count, open, requireExposures: true);
        Game active = save.Cursor == save.Units.Count
            ? complete.Game
            : SessionReplayVerification.Replay(save, save.Cursor, open, requireExposures: true).Game;
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
            : SessionReplayVerification.Replay(save, cursor, open, requireExposures: true).Game;
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
        _ = SessionReplayVerification.Replay(
            save,
            save.Cursor,
            open,
            requireExposures: true,
            history);
        return history;
    }

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
        ReplayResult replayed = SessionReplayVerification.Replay(save, save.Units.Count, open, requireExposures: true);
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

    internal static void RequireExposures(
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

    internal sealed record ReplayResult(
        Game Game,
        IReadOnlyList<IReadOnlyList<InformationExposure>> Exposures);

    internal sealed record ReplayDecision(
        IReadOnlyList<GameEvent> Events,
        IReadOnlyList<InformationExposure> Exposures);

    internal sealed record RewrittenDecision(
        string? Role,
        JournalStep Step,
        IReadOnlyList<InformationExposure> Exposures);

}
