using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Flipping a non-identity card — <c>rr:flip</c>.</summary>
public static class CardFlip
{
    /// <summary>Turns a card to a named face and applies the face-type lifecycle.</summary>
    public static void To(
        World world, ICardFacts facts, Card card, string face,
        string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(face);
        ArgumentNullException.ThrowIfNull(events);

        int destination = card.Faces.ToList().IndexOf(face);
        if (destination < 0)
        {
            throw new ArgumentException(
                $"card {card.ObjectId} has no face '{face}'", nameof(face));
        }

        CardKind before = facts.Kind(card.FaceId);
        CardKind after = facts.Kind(face);

        // `rr:flip.2.2`: a different card type discards every attached,
        // tucked, and status card. Preflight the whole hosted tree before the
        // face or any attachment moves, so an unsupported permanent
        // attachment cannot leave a half-flipped board behind.
        if (before != after)
        {
            Discard.PreflightAttachments(world, card);
            Discard.Attachments(world, card, trigger, events);

            // "All ... tokens are discarded from the card." Zero is the
            // engine's representation of an empty registered token pool.
            foreach (var (kind, held) in card.Tokens.ToList())
            {
                if (held <= 0)
                {
                    continue;
                }

                card.PlaceTokens(kind, -held);
                events.Add(new FieldSet(card.ObjectId, kind, held, 0)
                {
                    Trigger = trigger, Verb = "Flip",
                });
            }
        }

        // `rr:flip.2.1`: when the types match, nothing above runs, so attached
        // cards, tucked cards, status cards, and tokens all remain in place.
        card.TurnTo(face);
        events.Add(new CardsFlipped([card.ObjectId], true)
        {
            Trigger = trigger, Verb = "Flip",
        });
    }
}
