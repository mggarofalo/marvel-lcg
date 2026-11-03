using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>A retained encounter card and its explicit non-reveal placement instruction.</summary>
public sealed record EncounterEntryRequest(int Card, int Incarnation, int Area, int Player, CardEntryPlacement Placement)
{
    /// <summary>The batch whose last entry this entry's keyword activations and responses wait for.</summary>
    public MinionEntryBatch? Batch { get; init; }

    /// <summary>Capture the physical card before entry interrupts may move it.</summary>
    public static EncounterEntryRequest For(State.Card card, int player, CardEntryPlacement placement) =>
        new(card.ObjectId, card.Incarnation, card.Area.Id, player, placement);
}
