using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Ordinary play commitment and validation of an effect-scoped permanent play.</summary>
public static class ScopedPermanentPlay
{
    internal static void Play(World world, ScopedCardPlayRequest request, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (facts, abilities, seat, card, choice, reduction) = request;
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(choice);
        ArgumentNullException.ThrowIfNull(events);
        if (reduction is not null) ScopedPermanentPlay.Validate(world, request);
        var targets = choice.Targets;
        var paying = choice.Spent;

        // Admission checks precede committed changes; the accepted card is exposed
        // faceup outside play before any generator cost can open an interrupt.
        // Step 2. Play restrictions.
        if (!CardPlayLegality.Permitted(world, facts, seat, card, targets, abilities))
        {
            throw new RulesNotImplementedException(
                $"card {card.ObjectId} ('{card.FaceId}') in {card.Area.Type} cannot be played "
                + $"by {seat.Name} right now");
        }

        // Steps 3 and 4. Determine the cost and apply modifiers. The same
        // operation prices the affordance, so what was offered and what is
        // charged cannot disagree.
        var adjusted = CardPayment.CostOf(world, facts, seat, card,
            targets is { Count: 1 } ? targets[0] : null, reduction);

        // Step 5. Pay it -- "if this step is reached and the cost(s) cannot be
        // paid, **abort this process without paying any costs**", so the whole
        // payment is checked before a single card is discarded.
        // `rr:alliance` -- for an alliance card this is every player's hand,
        // and for every other card it is only this player's.
        CardPlayPayment.Play(world,
            new CardPlayInitiation(facts, abilities, seat, card, adjusted, [.. targets], [.. paying])
            { Allocations = [.. choice.Allocated] }, events);
    }

    /// <summary>Rechecks the current price, restrictions, host and payment before commitment.</summary>
    public static void Validate(World world, ScopedCardPlayRequest request)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(request);
        var (facts, abilities, seat, card, choice, reduction) = request;
        if (!IsPlayable(world, request))
            throw new ArgumentException("The selected permanent can no longer be played.", nameof(request));

        int? host = choice.Targets.Count == 1 ? choice.Targets[0] : null;
        var price = CardPayment.Price(world, facts, seat, card, host, reduction);
        if (price is null || !ResourcePayment.Allows(price, choice.Spent, choice.Values, choice.Allocated))
            throw new ArgumentException("The selected payment no longer pays the current cost.", nameof(request));

        try
        {
            _ = CardPlayPayment.Prepare(world, new CardPlayInitiation(facts, abilities, seat, card,
                CardPayment.CostOf(world, facts, seat, card, host, reduction), choice.Targets, choice.Spent)
                { Allocations = choice.Allocated });
        }
        catch (RulesNotImplementedException rejected)
        {
            throw new ArgumentException(rejected.Message, nameof(request), rejected);
        }
    }
    private static bool IsPlayable(World world, ScopedCardPlayRequest request)
    {
        var (facts, abilities, seat, card, choice, reduction) = request;
        return reduction is not null && card.Area == seat.Hand && !choice.IsDecline
            && EffectiveCards.Kind(card, facts) != CardKind.Event
            && CardPlayLegality.Permitted(world, facts, seat, card, choice.Targets, abilities);
    }

}
