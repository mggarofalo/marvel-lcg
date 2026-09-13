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
