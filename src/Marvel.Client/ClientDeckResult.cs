using Marvel.Protocol;

namespace Marvel.Client;

/// <summary>A validated read-only deck response or a recoverable request error.</summary>
public sealed record ClientDeckResult(DeckResponse? Decks, ClientStartupError? Error)
{
    /// <summary>Whether the host returned the requested complete result.</summary>
    public bool Succeeded => Decks is not null && Error is null;
}
