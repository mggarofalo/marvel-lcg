using System.Collections.Immutable;

namespace Marvel.Rules.Setup;

/// <summary>An ordered full player deck, including its identity's signature cards.</summary>
public sealed record PlayerDeckRecipe
{
    /// <summary>Captures the caller's ordering independently of its mutable collection.</summary>
    public PlayerDeckRecipe(IReadOnlyList<string> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        if (cards.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("deck entries must name card faces", nameof(cards));
        Cards = cards.ToImmutableArray();
    }

    /// <summary>Physical card face specifications in deterministic creation order.</summary>
    public IReadOnlyList<string> Cards { get; }
}
