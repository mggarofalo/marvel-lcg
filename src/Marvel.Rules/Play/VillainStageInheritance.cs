using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

internal static class VillainStageInheritance
{
    /// <summary>
    /// What a new villain stage keeps from the old one —
    /// <c>rr:villain-defeat.3</c> and <c>.4</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two clauses are the same list with opposite answers, and the title
    /// is what chooses between them. <b>Same title</b> (<c>.3.2</c>):
    /// "attachments, upgrades, status cards, counters, and non-damage tokens on
    /// a villain carry over to the new stage." <b>Different title</b>
    /// (<c>.4.2</c>): they "do <b>not</b> carry over".
    /// </para>
    /// <para>
    /// Rhino's three stages share a title, and Charge attaches to Rhino — so
    /// this is the ordinary case in the one scenario the engine plays, not an
    /// expansion corner.
    /// </para>
    /// <para>
    /// <b>Non-damage tokens.</b> Damage is not a token here (<c>Card.Damage</c>
    /// is its own field, because the digest records remaining <c>health</c> and
    /// no damage key), and <c>rr:villain-defeat.2</c> says excess damage does
    /// not carry over anyway — so every token the old stage held is one that
    /// travels.
    /// </para>
    /// </remarks>
    internal static void Apply(
        World world, ICardFacts facts, Card was, Card now, string trigger,
        List<GameEvent> events, IReadOnlyDictionary<string, long> carriedTokens)
    {
        bool same = string.Equals(
            facts.Title(was.FaceId), facts.Title(now.FaceId), StringComparison.Ordinal);

        foreach (var area in world.Areas.ToList())
        {
            if (area.Host != was.ObjectId || area.Cards.Count == 0)
            {
                continue;
            }

            var onto = world.AreaOf(area.Type, now.Area.PlayArea, now.ObjectId, area.CardOwner);
            foreach (var card in area.Cards.ToList())
            {
                if (!same)
                {
                    Discard.Card(world, card, trigger, events);
                    continue;
                }

                var from = card.Area;
                World.MoveToTop(card, onto);
                events.Add(new CardsMoved(
                    Places.Reference(from), Places.Reference(onto),
                    [new Landing(card.ObjectId, onto.Cards.Count - 1)])
                {
                    Trigger = trigger,
                    Verb = "Carry_Over",
                });
                events.Add(new CardAttached(card.ObjectId, now.ObjectId)
                {
                    Trigger = trigger,
                    Verb = "Carry_Over",
                });
            }
        }

        if (!same)
        {
            return;
        }

        foreach (var (kind, count) in carriedTokens)
        {
            if (count > 0)
            {
                now.PlaceTokens(kind, count);
                events.Add(new FieldSet(now.ObjectId, kind, 0, count)
                {
                    Trigger = trigger,
                    Verb = "Carry_Over",
                });
            }
        }
    }
}
