using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Villain-phase steps while the players are in separate game areas.</summary>
/// <remarks>
/// <para>
/// <c>pack:mc11:game-areas</c>: "Cards and components in one game area cannot
/// affect another game area (with the exception of the text on stage 2B)." and
/// "Play still proceeds in turn order". Ruling 97d62cdb9ce436a7 orders each step
/// across the areas: "each player should place threat on their main scheme in
/// turn order, then each player should resolve the activation of the Kang in
/// their game area in turn order, then each player should be dealt an encounter
/// card in turn order".
/// </para>
/// <para>
/// The table is separated exactly when the villain's play area belongs to no game
/// area: Kang's stage 2A detaches it and leaves 2B there, central. In an ordinary
/// game every play area shares the one game area, and none of this applies.
/// </para>
/// </remarks>
internal static class SeparateGameAreas
{
    internal static bool Active(World world) => world.GameAreaOf(PlayArea.Villains) is null;

    /// <summary>The game areas holding players, in turn order of their earliest player.</summary>
    /// <remarks>
    /// The ruling speaks of one main scheme per player. After a join, two players
    /// share one scheme; the engine chooses to place that scheme's threat once, at
    /// the turn of the area's earliest player, as the scheme's acceleration field
    /// is placed once per villain phase (<c>rr:villain-phase.1</c>).
    /// </remarks>
    internal static IReadOnlyList<(GameArea Area, int Player)> InTurnOrder(World world)
    {
        var ordered = new List<(GameArea, int)>();
        foreach (int player in world.PlayerOrder)
        {
            var area = world.GameAreaOf(PlayArea.Of(player));
            if (area is not null && ordered.All(item => !ReferenceEquals(item.Item1, area)))
                ordered.Add((area, player));
        }
        return ordered;
    }

    /// <summary>Schedules step 1 for each game area's own main scheme, in turn order.</summary>
    internal static void ScheduleStepOne(World world, ICardFacts facts)
    {
        var batches = new List<ThreatBatch>();
        foreach (var (area, player) in InTurnOrder(world))
        {
            var scheme = SchemeOf(world, area);
            long amount = StateFields.Modified(world, scheme, "escalation_threat", facts, world.Players)
                + Acceleration(world, facts, area);
            if (amount > 0)
                batches.Add(new ThreatBatch([scheme], null, amount, ThreatCause.VillainPhase,
                    "villain phase, place threat", player));
        }
        Threat.ScheduleEach(world, batches);
    }

    private static Card SchemeOf(World world, GameArea area)
    {
        var schemes = world.Cards.Where(card => card.Area.Type == DeckType.MainSchemesArea
            && ReferenceEquals(Places.GameAreaOf(world, card), area)).ToArray();
        return schemes.Length == 1 ? schemes[0]
            : throw new RulesNotImplementedException(
                $"villain phase step 1 requires one main scheme in game area {area.Id}");
    }

    /// <summary>Acceleration icons and tokens on cards in one game area.</summary>
    /// <remarks>
    /// <c>rr:acceleration-token.2.1</c> lets a token beside the main scheme add its
    /// threat; under the partition a token or icon adds threat only to its own
    /// area's scheme (MARVEL-894 Q1). Tokens on central 2B add none during the
    /// split, because 2B has no threat values, and carry into 4A.
    /// </remarks>
    internal static long Acceleration(World world, ICardFacts facts, GameArea area) =>
        InPlay(world, area).Sum(card => StateFields.Modified(world, card, "acceleration_icon", facts, world.Players)
            + card.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));

    /// <summary>Step 3's additional cards: each area's hazard icons deal to that area's players.</summary>
    /// <remarks>
    /// <c>rr:hazard-icon</c>: "for each hazard icon on cards in play, deal one player
    /// one additional card". An icon in one game area cannot affect a player in
    /// another, so each area's icons go round its own players in player order,
    /// starting with the earliest. Areas take their turn in turn order; that order
    /// between areas is the engine's choice under the turn-order instruction.
    /// </remarks>
    internal static void DealHazards(World world, ICardFacts facts, List<GameEvent> events)
    {
        foreach (var (area, _) in InTurnOrder(world))
        {
            long icons = InPlay(world, area).Sum(card => StateFields.Modified(world, card, "hazard", facts, world.Players));
            int[] players = [.. world.PlayerOrder.Where(player => area.Contains(PlayArea.Of(player)))];
            for (long dealt = 0; dealt < icons; dealt++)
                if (Deal.EncounterCard(world, players[(int)(dealt % players.Length)], "hazard", events) is null)
                    return;
        }
    }

    /// <summary>Where a side scheme enters play for <paramref name="player"/> while the table is separated.</summary>
    /// <remarks>
    /// MARVEL-894 Q15: <c>rr:side-scheme</c> puts a side scheme "next to the main
    /// scheme", the player's stage 3B stands in for that scheme, and
    /// <c>pack:mc11:game-areas</c> says side schemes "become part of the game
    /// area that you join", sharing only the encounter deck and discard pile.
    /// </remarks>
    internal static Area SideSchemeArea(World world, Card card, int player) =>
        player >= 0 && world.GameAreaOf(PlayArea.Of(player)) is not null
            ? world.AreaOf(DeckType.SideSchemesArea, PlayArea.Of(player))
            : throw new RulesNotImplementedException(
                $"side scheme '{card.FaceId}' enters play with no player game area while the table is separated");

    private static IEnumerable<Card> InPlay(World world, GameArea area) =>
        world.Areas.Where(zone => DeckTypes.IsInPlay(zone.Type)).SelectMany(zone => zone.Cards)
            .Where(card => ReferenceEquals(Places.GameAreaOf(world, card), area));
}
