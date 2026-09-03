using Marvel.Rules.Play;
using Marvel.Server;

namespace Marvel.Client;

/// <summary>The visibly distinct state of the local table.</summary>
public enum GameProgressKind
{
    /// <summary>The authoritative prompt is accepting one decision.</summary>
    AwaitingDecision,

    /// <summary>The authoritative game is waiting for another player's decision.</summary>
    WaitingForOtherPlayer,

    /// <summary>One mutation was sent and its outcome is not yet known.</summary>
    Resolving,

    /// <summary>The client is reading the current authoritative table.</summary>
    Synchronizing,

    /// <summary>A mutation was rejected before it could reach the engine.</summary>
    DecisionNotSent,

    /// <summary>A read-only synchronization failed without changing mutation certainty.</summary>
    SynchronizationUnavailable,

    /// <summary>A rejected mutation needs a fresh table before input resumes.</summary>
    DecisionRejected,

    /// <summary>The players defeated the final villain stage.</summary>
    PlayersWin,

    /// <summary>The villain completed the final main scheme.</summary>
    VillainWins,

    /// <summary>The players lost without the villain winning.</summary>
    PlayersLose,

    /// <summary>An error occurred, but a subsequent sync recovered the table.</summary>
    Recovered,

    /// <summary>A sent mutation has no authoritative result and cannot be repeated.</summary>
    Unconfirmed,

    /// <summary>The product could not open or load a table.</summary>
    Unavailable,

    /// <summary>The configured game service cannot currently be reached.</summary>
    ServiceUnavailable,

    /// <summary>The service rejected this client's wire protocol.</summary>
    VersionMismatch,

    /// <summary>The service established that the held session is no longer usable.</summary>
    SessionUnavailable,

    /// <summary>The service could not durably store the requested change.</summary>
    StorageFailure,
}

/// <summary>Copy and input policy for one current table state.</summary>
public sealed record GameProgressPresentation(
    GameProgressKind Kind,
    string Title,
    string Description,
    string Status,
    bool LocksDecisions,
    ClientStartupError? OperationalLock = null)
{
    /// <summary>Describes one complete authoritative response.</summary>
    public static GameProgressPresentation FromResponse(EngineResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(response.World);
        return response.World.Outcome switch
        {
            Outcome.Unfinished when response.Prompt is null => new(
                GameProgressKind.WaitingForOtherPlayer,
                "Waiting for another player.",
                $"{response.World.Areas.Count} visible areas · "
                    + $"{response.Events.Count} new events",
                "GAME IN PROGRESS  ·  WAITING FOR ANOTHER PLAYER",
                LocksDecisions: true),
            Outcome.Unfinished => new(
                GameProgressKind.AwaitingDecision,
                "Your move.",
                $"{response.World.Areas.Count} visible areas · "
                    + $"{response.Events.Count} new events",
                "DECISION READY  ·  "
                    + Humanize(response.Prompt!.Asking.ToString()).ToUpperInvariant(),
                LocksDecisions: false),
            Outcome.PlayersWin => new(
                GameProgressKind.PlayersWin,
                "Victory.",
                "The players defeated the final villain stage.",
                "GAME COMPLETE  ·  PLAYERS WIN",
                LocksDecisions: true),
            Outcome.VillainWins => new(
                GameProgressKind.VillainWins,
                "Defeat.",
                "The villain completed the final main scheme.",
                "GAME COMPLETE  ·  VILLAIN WINS",
                LocksDecisions: true),
            Outcome.PlayersLose => new(
                GameProgressKind.PlayersLose,
                "Defeat.",
                "The players lost when the encounter could not continue.",
                "GAME COMPLETE  ·  PLAYERS LOSE",
                LocksDecisions: true),
            _ => throw new ArgumentOutOfRangeException(
                nameof(response), response.World.Outcome, "unknown game outcome"),
        };
    }

    /// <summary>Applies a read-only snapshot without clearing an operator lock.</summary>
    public static GameProgressPresentation FromSynchronization(
        EngineResponse response,
        GameProgressPresentation? prior) =>
        prior?.OperationalLock is { } blocked
            ? Recovered(response, blocked)
            : FromResponse(response);

    /// <summary>Shows that one mutation is in flight and cannot be repeated.</summary>
    public static GameProgressPresentation Resolving() => new(
        GameProgressKind.Resolving,
        "Resolving decision…",
        "The engine is committing the selected action.",
        "DECISION SENT  ·  WAITING FOR THE AUTHORITATIVE TABLE",
        LocksDecisions: true);

    /// <summary>Locks decisions while the current authoritative table is requested.</summary>
    public static GameProgressPresentation Synchronizing() => new(
        GameProgressKind.Synchronizing,
        "Synchronizing table…",
        "The client is requesting the current authoritative table.",
        "SYNCHRONIZING  ·  WAITING FOR THE AUTHORITATIVE TABLE",
        LocksDecisions: true);

    /// <summary>Unlocks a preserved draft when the mutation was never sent.</summary>
    public static GameProgressPresentation DecisionNotSent(ClientStartupError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(
            GameProgressKind.DecisionNotSent,
            "Decision not sent.",
            error.Message + " Your selection is preserved and is safe to retry.",
            $"NOT SENT  ·  RETRY SAFE  ·  {error.Code.ToUpperInvariant()}",
            LocksDecisions: false);
    }

    /// <summary>Preserves the prior input policy after a read-only sync failure.</summary>
    public static GameProgressPresentation SynchronizationUnavailable(
        ClientStartupError error,
        bool locksDecisions,
        ClientStartupError? operationalLock = null)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (operationalLock is not null)
        {
            return Unavailable(operationalLock) with
            {
                Description = operationalLock.Message + " " + error.Message
                    + " The last authoritative table remains displayed.",
            };
        }

        if (error.Code == "unsupported_version" || IsStorageFailure(error.Code))
        {
            return Unavailable(error);
        }

        return new(
            GameProgressKind.SynchronizationUnavailable,
            "Table not synchronized.",
            error.Message + " The last authoritative table remains displayed.",
            $"SYNC READ FAILED  ·  {error.Code.ToUpperInvariant()}",
            locksDecisions);
    }

    /// <summary>Locks a rejected decision until its current table can be read.</summary>
    public static GameProgressPresentation DecisionRejected(ClientStartupError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (IsStorageFailure(error.Code))
        {
            return Unavailable(error) with
            {
                Description = error.Message
                    + " Input remains locked while the last displayed table is preserved.",
            };
        }

        return new(
            GameProgressKind.DecisionRejected,
            "Decision rejected.",
            error.Message + " Synchronize the current table before taking another action.",
            $"DECISION REJECTED  ·  SYNCHRONIZATION REQUIRED  ·  {error.Code.ToUpperInvariant()}",
            LocksDecisions: true);
    }

    /// <summary>Preserves an authoritative recovered table while explaining its error.</summary>
    public static GameProgressPresentation Recovered(
        EngineResponse response,
        ClientStartupError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (IsStorageFailure(error.Code))
        {
            return Unavailable(error) with
            {
                Description = error.Message
                    + " The last authoritative table was recovered, but input remains locked.",
                OperationalLock = error,
            };
        }

        GameProgressPresentation current = FromResponse(response);
        return current.Kind switch
        {
            GameProgressKind.AwaitingDecision => new GameProgressPresentation(
                GameProgressKind.Recovered,
                "Table recovered.",
                error.Message,
                $"CURRENT DECISION RESTORED  ·  {error.Code.ToUpperInvariant()}",
                LocksDecisions: false),
            GameProgressKind.WaitingForOtherPlayer => current with
            {
                Description = current.Description + " The table was recovered after: "
                    + error.Message,
            },
            _ => current with
            {
                Description = current.Description + " The final table was recovered after: "
                    + error.Message,
            },
        };
    }

    /// <summary>Locks a table whose sent mutation could not be reconciled.</summary>
    public static GameProgressPresentation Unconfirmed(ClientStartupError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(
            GameProgressKind.Unconfirmed,
            "Table unconfirmed.",
            error.Message + " Restart or reconnect before taking another action.",
            $"MUTATION NOT REPEATED  ·  {error.Code.ToUpperInvariant()}",
            LocksDecisions: true);
    }

    /// <summary>Shows a setup or product failure rather than a gameplay ending.</summary>
    public static GameProgressPresentation Unavailable(ClientStartupError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        // These are product-operational states chosen by this project. They
        // describe evidence in the wire response, never inferred game state.
        return error.Code switch
        {
            "transport_unavailable" => new(
                GameProgressKind.ServiceUnavailable,
                "Game service unavailable.",
                error.Message,
                "SERVICE UNAVAILABLE  ·  RECONNECT WHEN THE SERVER IS READY",
                LocksDecisions: true),
            "unsupported_version" => new(
                GameProgressKind.VersionMismatch,
                "Client and server versions do not match.",
                error.Message,
                "VERSION MISMATCH  ·  UPDATE THE CLIENT OR SERVER",
                LocksDecisions: true),
            "session_unavailable" or "session_not_found" or "invitation_unavailable" => new(
                GameProgressKind.SessionUnavailable,
                "Session or invitation unavailable.",
                error.Message,
                "SESSION UNAVAILABLE  ·  RETURN TO JOIN",
                LocksDecisions: true),
            _ when IsStorageFailure(error.Code) => new(
                    GameProgressKind.StorageFailure,
                "Server storage unavailable.",
                error.Message,
                "STORAGE FAILURE  ·  OPERATOR ACTION REQUIRED",
                    LocksDecisions: true,
                    OperationalLock: error),
            _ => new(
                GameProgressKind.Unavailable,
                "Game unavailable.",
                error.Message,
                $"PRODUCT ERROR  ·  {error.Code.ToUpperInvariant()}",
                LocksDecisions: true),
        };
    }

    private static bool IsStorageFailure(string code) => code is
        "save_failed" or "persistence_failed" or "restore_failed"
        or "unsupported_downgrade";

    internal static string Humanize(string value)
    {
        var result = new System.Text.StringBuilder(value.Length + 8);
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];
            if (index > 0 && (current == '_'
                || char.IsUpper(current) && char.IsLower(value[index - 1])))
            {
                result.Append(' ');
            }

            if (current != '_')
            {
                result.Append(current);
            }
        }
        return result.ToString();
    }
}
