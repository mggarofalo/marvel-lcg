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
        World world, ICardFacts facts, Seat seat, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);

        long amount = Resources.Cost(card.FaceId, facts, world.Players) ?? 0;
        var modifiers = world.Effects.Active().Where(effect =>
            string.Equals(effect.Kind, CardCostReduction, StringComparison.Ordinal)
            && effect.Affects == seat.IdentityCard.ObjectId
            && effect.Amount > 0).ToList();

        foreach (var modifier in modifiers)
        {
            amount = Math.Max(0, amount - modifier.Amount);
        }

        return new AdjustedCardCost(amount, modifiers);
    }

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

    /// <summary>Every resource source supplied by one explicit card capability.</summary>
    public static IReadOnlyList<ResourceSource> Generators(
        World world, ICardFacts facts, Seat seat,
        IResourceCardAbilities resourceAbilities, Card? payingFor = null)
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
            world, seat.Index));

        foreach (var card in seat.Hand.Cards)
        {
            string generates = resourceAbilities.ResourcesGeneratedBy(world, card, payingFor);
            if (generates.Length > 0)
            {
                sources.Add(new ResourceSource(card.ObjectId, generates));
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
    public static CostOption? Price(World world, ICardFacts facts, Seat seat, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);

        if (!Permitted(world, facts, seat, card, abilities: world.CardPlayAbilities))
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
        if (facts.Kind(card.FaceId) == CardKind.Event)
        {
            return null;
        }

        long cost = CostOf(world, facts, seat, card).Amount;

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
        string pool = string.Concat(sources.SelectMany(source => source.Generates));
        return !Resources.Pays(pool, cost, Resources.Required(world, card, facts))
            ? null
            : new CostOption(
                Target: card.ObjectId,
                Cost: cost.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Sources: sources);
    }
}
