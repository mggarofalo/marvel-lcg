using Marvel.Rules.Events;
using Marvel.Rules.State;
using static Marvel.Rules.Play.CardPlayLegality;
using static Marvel.Rules.Play.CardEntry;

namespace Marvel.Rules.Play;

/// <summary>Owns card play under a permission that ignores the resource cost.</summary>
internal static class FreeCardPlay
{
    internal static void Play(
        World world, ICardFacts facts, ICardPlayAbilities abilities, Seat seat, Card card,
        List<GameEvent> events, IReadOnlyList<int>? targets = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        if (EffectiveCards.Kind(card, facts) == CardKind.Event)
        {
            throw new RulesNotImplementedException(
                $"event '{card.FaceId}' cannot be played ignoring its resource cost "
                + "until its Action selection can be represented");
        }

        if (!Permitted(world, facts, seat, card, targets, abilities))
        {
            throw new RulesNotImplementedException(
                $"card {card.ObjectId} ('{card.FaceId}') cannot be played ignoring its cost");
        }

        // `rr:requirement-resources.2`: ignoring the cost generates and pays
        // no resources, so a printed requirement makes this permission
        // unusable. Refuse before the card leaves its hand.
        if (Resources.Required(world, card, facts).Length > 0)
        {
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' has a resource requirement and cannot be played "
                + "ignoring its resource cost");
        }

        // `rr:ignore.1`: zero resources are considered paid. Cost reductions
        // are neither applied nor consumed because there is no resource cost
        // in effect during this play.
        Enter(world, facts, abilities, seat, card, events, targets ?? []);
    }
}
