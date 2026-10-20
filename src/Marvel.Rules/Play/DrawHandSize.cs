using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Counts each drawn obligation against a draw-to-hand-size instruction.</summary>
internal static class DrawHandSize
{
    internal static void Resolve(World world, ICardFacts facts, int seat, string trigger, List<GameEvent> events)
    {
        var player = world.Seats[seat];
        // Re-read the live hand-size modifier after each entry, while bounding
        // this instruction by its initial limit. An obligation consumes a draw.
        long limit = PhaseEnd.HandSize(world, player, facts);
        long diverted = 0;
        for (long drawn = 0; drawn < limit; drawn++)
        {
            if (player.Hand.Cards.Count + diverted >= PhaseEnd.HandSize(world, player, facts)) break;
            int before = player.Hand.Cards.Count;
            if (Draw.Resolve(world, seat, 1, trigger, events) == 0) break;
            if (player.Hand.Cards.Count == before) diverted++;
        }
    }
}
