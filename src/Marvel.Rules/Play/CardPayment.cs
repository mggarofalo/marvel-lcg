using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Rules.Play.CardPayment;
using static Marvel.Rules.Play.CardPlayLegality;
using static Marvel.Rules.Play.CardControlTransfer;
using static Marvel.Rules.Play.CardEntry;
using static Marvel.Rules.Play.CardPlay;

namespace Marvel.Rules.Play;

/// <summary>Calculates and commits resource payment for playing cards.</summary>
public static class CardPayment
{
    /// <summary>Register a one-use reduction for the next card the player plays.</summary>
    public static ContinuousEffects.Registration ReduceNextCardCost(
        World world, Card source, int player, long amount)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(player, world.Players);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);

        return world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            CardCostReduction,
            Amount: amount,
            Card: source.ObjectId,
            Affects: world.Seats[player].IdentityCard.ObjectId,
            Lasts: new Duration(Until: TimingPoints.EndOfPlayerPhase, Uses: 1)));
    }

    /// <summary>Reduces the next matching card played by any player this phase.</summary>
    public static ContinuousEffects.Registration ReduceNextMatchingCardCost(
        World world, Card source, long amount, CardCostFilter filter)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        return world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, CardCostReduction, Amount: amount,
            Card: source.ObjectId,
            Lasts: new Duration(Until: TimingPoints.EndOfPhase, Uses: 1))
        { CostFilter = filter });
    }

    /// <summary>
    /// Determines one card's modified resource cost and remembers the effects
    /// that supplied the adjustment.
    /// </summary>
    /// <remarks>
    /// <c>rr:initiating-abilities.step.3</c> determines the cost and step 4
    /// applies modifiers before step 5 pays it. Cost cannot become negative;
    /// each reduction stops at zero. This method does not consume anything,
    /// because pricing a card is not playing it.
    /// </remarks>
    public static AdjustedCardCost CostOf(
        World world, ICardFacts facts, Seat seat, Card card, int? attachmentHost = null) =>
        CostOf(world, facts, seat, card, attachmentHost, null);

    /// <summary>Prices one play with an explicitly scoped resource-cost reduction.</summary>
    public static AdjustedCardCost CostOf(
        World world, ICardFacts facts, Seat seat, Card card, int? attachmentHost,
        ScopedCardCostReduction? reduction)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);

        long amount = Resources.Cost(card.FaceId, facts, world.Players) ?? 0;
        var modifiers = world.Effects.Active().Where(effect =>
            string.Equals(effect.Kind, CardCostReduction, StringComparison.Ordinal)
            && (effect.Affects == seat.IdentityCard.ObjectId
                || effect.Affects is null && effect.CostFilter is not null)
            && Matches(world, card, effect.CostFilter)
            && effect.Amount > 0).ToList();

        var increases = NextEventCost.Applied(world, seat, card);
        amount = checked(amount + increases.Sum(effect => effect.Amount));
        long reduced = amount;
        foreach (var modifier in modifiers)
        {
            amount = Math.Max(0, amount - modifier.Amount);
        }

        long self = world.CardPlayAbilities.SelfPlayCostReduction(world, card, seat.Index);
        amount = Math.Max(0, amount - self);
        amount = AllyPlayHistory.AdjustCost(world, card, amount);
        amount = UpgradeHostCost.Adjust(world, card, attachmentHost, amount);
        if (reduction is not null)
        {
            reduction.Validate(world, seat, card);
            amount = Math.Max(0, amount - reduction.Amount);
        }
        long reductions = checked(modifiers.Sum(modifier => modifier.Amount) + self + (reduction?.Amount ?? 0));
        return new AdjustedCardCost(amount, [.. modifiers, .. increases])
        {
            VariableReduction = VariableCardCost.Remaining(card, facts, reductions, reduced),
        };
    }

    private static bool Matches(World world, Card card, CardCostFilter? filter) =>
        filter is null || (filter.Kind is null || EffectiveCards.Kind(card, world.Facts) == filter.Kind)
            && (filter.Trait is null || Traits.Has(world, card, filter.Trait, world.Facts));

    /// <summary>Consumes the one-use effects applied to a successfully played card.</summary>
    public static void UseCostModifiers(World world, AdjustedCardCost cost)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(cost);

        foreach (var modifier in cost.Modifiers)
        {
            world.Effects.Use(modifier);
        }
    }

    /// <summary>
    /// Every card in a hand that can be spent for resources.
    /// </summary>
    /// <remarks>
    /// <c>rr:resource.1</c> — "discarding cards from their hand to generate the
    /// resource or resources indicated at the bottom-left corner of the card".
    /// A card printing nothing there generates nothing and is not a generator.
    /// <para><c>rr:resource-ability</c>'s resource abilities are included from
    /// cards in play beside these hand cards.</para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="seat">Whose hand.</param>
    /// <param name="payingFor">The card being paid for, or null for an ability cost.</param>
    public static IReadOnlyList<ResourceSource> Generators(
        World world, ICardFacts facts, Seat seat, Card? payingFor = null)
        => Generators(world, facts, seat, world.ResourceAbilities, payingFor);

    /// <summary>Every eligible source from one card capability. Ability source restricts generators without applying card-play multipliers.</summary>
    public static IReadOnlyList<ResourceSource> Generators(
        World world, ICardFacts facts, Seat seat,
        IResourceCardAbilities resourceAbilities, Card? payingFor = null, Card? abilitySource = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(resourceAbilities);

        // `rr:resource-ability.1` -- one "can be triggered **anytime the player
        // who controls the ability is generating resources to pay a cost**", so
        // it belongs beside the cards in hand: another way to make a resource
        // rather than another moment. Peter Parker's "Scientist" is the one the
        // recorded prompt carries, and it is why that prompt lists six
        // generators for five payable cards.
        var sources = new List<ResourceSource>(resourceAbilities.ResourceAbilities(
            world, seat.Index).Where(source => ResourceSourceEligibility.Allows(
                world, facts, source, payingFor ?? abilitySource)));

        foreach (var card in seat.Hand.Cards)
        {
            if (!resourceAbilities.CanUseHandResources(world, card)) continue;
            var quote = resourceAbilities.QuoteHandResources(world, card, payingFor ?? abilitySource);
            if (quote.Generates.Length > 0)
            {
                sources.Add(quote);
            }
        }

        return sources;
    }

    /// <summary>
    /// Whose hands may pay for one card — <c>rr:alliance</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "When a player declares their intention to play a card with the alliance
    /// keyword, <b>any player(s) may help pay the costs</b> for that card",
    /// which <c>.1</c> writes as the constant ability "while paying costs for
    /// this card, any player may contribute to paying those costs".
    /// </para>
    /// <para>
    /// <c>rr:alliance.2</c> is the limit of it: "only the player playing the
    /// card with the alliance keyword is considered to be resolving that
    /// card". Helping to pay is not playing — the card is still the one
    /// player's, and everything downstream of the payment reads the seat that
    /// played it.
    /// </para>
    /// <para>
    /// Every spent card goes to <b>its own owner's</b> discard pile, which
    /// <see cref="Discard.Card"/> already does by reading the card rather than
    /// the player who spent it.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="seat">Who is playing the card.</param>
    /// <param name="card">The card being paid for.</param>
    public static IReadOnlyList<Seat> Paying(
        World world, ICardFacts facts, Seat seat, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);

        return StateFields.Modified(world, card, "alliance", facts, world.Players) > 0
            ? [.. world.PlayerOrder.Select(index => world.Seats[index])]
            : [seat];
    }

    /// <summary>
    /// What a card in hand costs, and what could pay for it — or null when it
    /// cannot be played at all.
    /// </summary>
    /// <remarks>
    /// <c>rr:initiating-abilities.step.2</c> and <c>.step.3</c>: the play
    /// restrictions are checked first, then the cost and "the player's ability
    /// to pay them". A card that fails either is not offered — an affordance
    /// that would throw when taken is worse than an absent one.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="seat">Whose card.</param>
    /// <param name="card">The card in hand.</param>
    /// <param name="attachmentHost">The selected physical attachment host, when pricing one destination.</param>
    public static CostOption? Price(World world, ICardFacts facts, Seat seat, Card card, int? attachmentHost = null) =>
        Price(world, facts, seat, card, attachmentHost, null);

    /// <summary>Prices a permanent play using a reduction scoped to that card.</summary>
    public static CostOption? Price(World world, ICardFacts facts, Seat seat, Card card,
        int? attachmentHost, ScopedCardCostReduction? reduction)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);

        if (!Permitted(world, facts, seat, card, attachmentHost is { } host ? [host] : null, world.CardPlayAbilities))
        {
            return null;
        }

        // **Events are not a turn option on their own.** `rr:player-turn.5`
        // reaches them through "trigger an **Action** ability on an event card
        // in their hand", so an event without one is played in a window and not
        // here -- 555 of the 602 events in the pool have no Action ability at
        // all. An authored Action event is offered by `ICardActionAbilities.Actions`
        // instead, which plays it while resolving that action.
        //
        // The recorded board is the check: its opening hand holds `01003`
        // Backflip, whose ability is an **Interrupt (defense)**, and the
        // recording does not offer it.
        if (EffectiveCards.Kind(card, facts) == CardKind.Event)
        {
            return null;
        }

        if (attachmentHost is { } target)
        {
            var targets = CardPlayLegality.LegalAttachmentTargets(world, facts, seat, card, world.CardPlayAbilities);
            if (targets is null || !targets.Contains(target)) return null;
        }

        long cost = CostOf(world, facts, seat, card, attachmentHost, reduction).Amount;

        // The card being played cannot also pay for itself: `rr:cost.3` spends
        // resources "by discarding cards from their hand", and this one is
        // leaving the hand to be played.
        var sources = Paying(world, facts, seat, card)
            .SelectMany(paying => Generators(world, facts, paying, card))
            .Where(source => source.Effect != card.ObjectId)
            .ToList();

        // **Asked of the whole pool, and that is exactly the right question.**
        // `rr:cost.4` permits generating beyond the cost, so if every generator
        // together cannot pay then no choice among them can; and if they can,
        // spending all of them is a payment. The types matter as well as the
        // count -- `rr:requirement-resources` makes a card with a requirement
        // unplayable without those resources, however many cards are in hand.
        var price = new CostOption(
            Target: attachmentHost ?? card.ObjectId,
            Cost: cost.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Sources: sources);
        var requiredPrice = price with { Rule = [Resources.Required(world, card, facts)] };
        return ResourcePayment.Allocate(requiredPrice, sources.Select(source => source.Effect).ToArray()) is null
            ? null : price;
    }
}
