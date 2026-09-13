using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Server;

/// <summary>The versioned request/response protocol shared by both transports.</summary>
public static class EngineProtocol
{
    /// <summary>
    /// The only protocol version this host accepts. It includes independently
    /// scoped seat capabilities, play-area topology events, setup discovery,
    /// per-target allocation capacities, procedural-card face facts, host
    /// revisions, replay-verified history cursor commands, and legal trace
    /// rewriting for committed action units. Version 12 adds visibility-safe
    /// completed action summaries at their undo cursor boundaries. Version 11
    /// exposes the product, replay, save and runtime-dataset identities during
    /// setup discovery.
    /// Version 15 adds structured table context, compact public player summaries,
    /// and visibility-reviewed relationship subjects for direct manipulation.
    /// Version 10 also tells clients
    /// when a wild-resource declaration is observable by the resolving effect.
    /// </summary>
    public const int Version = 15;

    /// <summary>The largest request or game id accepted or echoed.</summary>
    public const int MaximumIdentifierLength = 256;

    /// <summary>The largest diagnostic text returned to a client.</summary>
    public const int MaximumErrorLength = 1024;

    /// <summary>Starts a game from the named, vendored content.</summary>
    public const string Open = "open";

    /// <summary>Reads the authored choices from which a game can be opened.</summary>
    public const string Setup = "setup";

    /// <summary>Redeems a server-issued one-time invitation to a seat.</summary>
    public const string Attach = "attach";

    /// <summary>Reads the current prompt and snapshot without changing the game.</summary>
    public const string Sync = "sync";

    /// <summary>Applies one decision to an open game.</summary>
    public const string Resolve = "resolve";

    /// <summary>Reconstructs the table at an earlier editable history boundary.</summary>
    public const string Undo = "undo";

    /// <summary>Reconstructs the table through retained inactive history.</summary>
    public const string Redo = "redo";

    /// <summary>Rewrites committed action units in the requested legal order.</summary>
    public const string Reorder = "reorder";

    /// <summary>Releases an open game.</summary>
    public const string Close = "close";
}
