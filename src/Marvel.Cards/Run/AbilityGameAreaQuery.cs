using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Scenario references that resolve within the game area of the card making them.</summary>
/// <remarks>
/// <para>
/// pack:mc11:game-areas: "Cards and components in one game area cannot affect
/// another game area". MARVEL-894 (Q2) reads "Kang" on the Kang scenario's
/// encounter cards as the Kang villain in that card's game area (ruling
/// 5ecfc71ddff64d39 keeps a Kang boost local), and (Q4) "each obligation in
/// play" as the obligations in that game area. Immortus's "while a minion is in
/// play" is read the same way, as a minion in his game area; that reading is
/// ours, by the same partition.
/// </para>
/// <para>
/// Every answer comes from <see cref="Places.CanAffect"/> and nowhere else, so an
/// ordinary game, which has one game area holding every play area, reads the
/// whole table. While the table is split, a source in no game area raises: the
/// engine places a revealed side scheme in the villain's play area
/// (rr:reveal.5), which a split leaves in no game area, so such a card has no
/// game area of its own to read. More than one villain in reach also raises
/// rather than choosing.
/// </para>
/// </remarks>
internal static class AbilityGameAreaQuery
{
    /// <summary>The villain in the source's game area, or none.</summary>
    internal static IReadOnlyList<Card> Villain(AbilityQueryContext cast)
    {
        var anchor = Anchor(cast);
        var found = cast.World.Cards.Where(card => card.Area.Type == DeckType.VillainArea
            && Places.CanAffect(cast.World, anchor, card)).ToArray();
        return found.Length <= 1 ? found : throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' names the villain in its game area, and {found.Length} villains are there");
    }

    /// <summary>Every obligation in play in the source's game area, in object-id order.</summary>
    /// <remarks>
    /// rr:obligation.5 puts a revealed obligation in its player's play area, so an
    /// obligation is in play exactly while it is in a player's obligations area.
    /// </remarks>
    internal static IReadOnlyList<Card> Obligations(AbilityQueryContext cast)
    {
        var anchor = Anchor(cast);
        return [.. cast.World.Cards.Where(card => card.Area.Type == DeckType.ObligationsArea
            && Places.CanAffect(cast.World, anchor, card))];
    }

    /// <summary>Every minion in play in the source's game area, in object-id order.</summary>
    /// <remarks>
    /// rr:minion: "If a minion enters play, it engages the player who is
    /// revealing the card [...]", so a minion is in play exactly while it is in
    /// an engaged-enemies area.
    /// </remarks>
    internal static IReadOnlyList<Card> Minions(AbilityQueryContext cast)
    {
        var anchor = Anchor(cast);
        // rr:minion.3 activates the minions engaged with a player; nothing else is
        // engaged in admitted content, so the kind test mirrors the ordinary
        // minions query and is not otherwise observable today.
        return [.. cast.World.Cards.Where(card => card.Area.Type == DeckType.EngagedEnemiesArea
            && EffectiveCards.Kind(card, cast.World.Facts) == CardKind.Minion
            && Places.CanAffect(cast.World, anchor, card)).OrderBy(card => card.ObjectId)];
    }

    // A faceup boost card sits in the villain's play area, which a split table
    // leaves in no game area. rr:boost-boost-icon: "each boost card on the enemy
    // is turned face up", so the activating enemy's place stands for it. That
    // anchoring is our reading; ruling 5ecfc71ddff64d39 treats a boost as
    // belonging to the activation's game area.
    private static Card Anchor(AbilityQueryContext cast)
    {
        var anchor = cast.Source.Area.Type == DeckType.BoostingArea && cast.World.Activation is { } activation
            ? cast.World.Cards[activation.Enemy] : cast.Source;
        if (Places.GameAreaOf(cast.World, anchor) is null
            && cast.World.GameAreas.Count(area => area.PlayAreas.Count > 0) > 1)
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' is in no game area while the table is split, so its game area is undefined");
        return anchor;
    }
}
