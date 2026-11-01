using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Protocol;

/// <summary>The versioned request/response protocol shared by both transports.</summary>
public static class EngineProtocol
{
    /// <summary>
    /// The sole accepted wire contract: visibility-scoped prompts and events, explicit
    /// entry recovery claims, typed failure outcomes, and complete engine decisions.
    /// </summary>
    public const int Version = 46;

    /// <summary>Creates or advances an owner-authenticated persistent campaign.</summary>
    public const string Campaign = "campaign";

    /// <summary>Reads public deckbuilding facts or assesses a draft before game creation.</summary>
    public const string Decks = "decks";

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
