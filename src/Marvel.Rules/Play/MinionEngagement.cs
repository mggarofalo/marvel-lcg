using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>The physical minion and destination player of an actual engagement.</summary>
public static class MinionEngagement
{
    /// <summary>Engine-chosen condition spelling; Subject is the minion and Player the engaged seat.</summary>
    public const string Engaged = "WhenMinionEngaged";

    internal static void RecordEntry(Card card, ICardFacts facts, Occurrence? occurrence)
    {
        // rr:engage: "When a minion enters play in a player's play area, it engages that player."
        if (EffectiveCards.Kind(card, facts) == CardKind.Minion
            && card.Area.Type == DeckType.EngagedEnemiesArea)
            occurrence?.Also(Engaged);
    }

    internal static bool IsTransfer(World world, Card card, int player) =>
        EffectiveCards.Kind(card, world.Facts) == CardKind.Minion
        && card.Area.Type == DeckType.EngagedEnemiesArea
        && card.Area.PlayArea != PlayArea.Of(player);

    internal static bool Transfer(World world, Card card, int player,
        Occurrence occurrence, List<GameEvent> events)
    {
        // rr:engage.3: abilities cannot cause a minion to engage the same player again.
        if (!IsTransfer(world, card, player)) return false;
        var from = card.Area;
        var into = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player));
        World.MoveToTop(card, into);
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(into),
            [new Landing(card.ObjectId, into.Cards.Count - 1)])
        { Trigger = Engaged, Verb = "Engage" });
        occurrence.Also(Engaged);
        return true;
    }
}
