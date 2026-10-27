using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>The exact card and source area of one encounter revelation.</summary>
/// <remarks>Engine choice: dealt cards retain their queue as the immediate source.
/// The September 14, 2026 FFG clarification counts first-dealt cards as from the
/// encounter deck, including cards dealt from a player deck.</remarks>
public sealed record EncounterRevealOrigin(int Card, AreaRef From, bool Dealt = false)
{
    /// <summary>Whether this revelation qualifies as from the encounter deck.</summary>
    public bool FromEncounterDeck => Dealt || From.Zone == nameof(DeckType.EncounterDeck);
}
