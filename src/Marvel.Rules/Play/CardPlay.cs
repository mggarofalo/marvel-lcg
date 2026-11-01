using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Rules.Play.CardPayment;
using static Marvel.Rules.Play.CardPlayLegality;
using static Marvel.Rules.Play.CardControlTransfer;
using static Marvel.Rules.Play.CardEntry;

namespace Marvel.Rules.Play;

/// <summary>
/// Playing a card from hand — <c>rr:play-put-into-play</c>,
/// <c>rr:initiating-abilities</c>.
/// </summary>
/// <remarks>
/// <para>
/// "Playing a card involves paying the card's cost and placing the card in the
/// play area. This causes the card to enter play <i>(or, in the case of an
/// event card, to resolve its ability and be placed in the discard pile)</i>."
/// </para>
/// <para>
/// <c>rr:initiating-abilities</c> numbers seven steps and they are numbered
/// here, because the order is what the rule is: restrictions are checked before
/// the cost is worked out, the cost is worked out before it is paid, and
/// <b>step 5 aborts without paying anything</b> if it cannot all be paid.
/// </para>
/// </remarks>
public static class CardPlay
{
    /// <inheritdoc cref="CardPayment.ReduceNextCardCost(World, Card, int, long)"/>
    public static ContinuousEffects.Registration ReduceNextCardCost(
        World world, Card source, int player, long amount) =>
        CardPayment.ReduceNextCardCost(world, source, player, amount);

    /// <inheritdoc cref="CardPayment.CostOf(World, ICardFacts, Seat, Card, int?)"/>
    public static AdjustedCardCost CostOf(
        World world, ICardFacts facts, Seat seat, Card card) =>
        CardPayment.CostOf(world, facts, seat, card);

    /// <inheritdoc cref="CardPayment.UseCostModifiers"/>
    public static void UseCostModifiers(World world, AdjustedCardCost cost) =>
        CardPayment.UseCostModifiers(world, cost);

    /// <summary>Returns every resource generator that may pay for a card.</summary>
    public static IReadOnlyList<ResourceSource> Generators(
        World world, ICardFacts facts, Seat seat, Card? payingFor = null) =>
        CardPayment.Generators(world, facts, seat, payingFor);

    /// <summary>Returns generators supplied by an explicit card capability.</summary>
    public static IReadOnlyList<ResourceSource> Generators(
        World world, ICardFacts facts, Seat seat,
        IResourceCardAbilities resourceAbilities, Card? payingFor = null) =>
        CardPayment.Generators(world, facts, seat, resourceAbilities, payingFor);

    /// <inheritdoc cref="CardPayment.Paying"/>
    public static IReadOnlyList<Seat> Paying(
        World world, ICardFacts facts, Seat seat, Card card) =>
        CardPayment.Paying(world, facts, seat, card);

    /// <inheritdoc cref="CardPayment.Price(World, ICardFacts, Seat, Card, int?)"/>
    public static CostOption? Price(World world, ICardFacts facts, Seat seat, Card card) =>
        CardPayment.Price(world, facts, seat, card);

    /// <inheritdoc cref="CardPlayLegality.WithinPerPlayerLimit"/>
    public static bool WithinPerPlayerLimit(
        World world, ICardFacts facts, Seat seat, Card card, IReadOnlyList<int>? targets,
        ICardPlayAbilities abilities) =>
        CardPlayLegality.WithinPerPlayerLimit(world, facts, seat, card, targets, abilities);

    /// <inheritdoc cref="CardPlayLegality.LegalAttachmentTargets"/>
    public static IReadOnlyList<int>? LegalAttachmentTargets(
        World world, ICardFacts facts, Seat seat, Card card, ICardPlayAbilities abilities) =>
        CardPlayLegality.LegalAttachmentTargets(world, facts, seat, card, abilities);

    /// <inheritdoc cref="CardControlTransfer.TakeControl"/>
    public static void TakeControl(
        World world, ICardFacts facts, Card card, int player) =>
        CardControlTransfer.TakeControl(world, facts, card, player);

    /// <inheritdoc cref="CardControlTransfer.ReturnToOwnerControl"/>
    public static void ReturnToOwnerControl(World world, ICardFacts facts, Card card) =>
        CardControlTransfer.ReturnToOwnerControl(world, facts, card);

    /// <summary>The affordance verb for playing a card.</summary>
    /// <remarks>
    /// <c>rr:player-turn.2</c> is "play a card", so the verb is <c>Play</c>.
    /// The most common one on the wire by a wide margin, and a client renders
    /// it, so it is a contract like the basic powers in
    /// <see cref="BasicPowers"/>.
    /// </remarks>
    public const string Verb = "Play";

    /// <summary>The lasting-effect kind for reducing a player's next card cost.</summary>
    public const string CardCostReduction = "cardCostReduction";

    /// <summary>
    /// Makes the next card one player plays this phase cost less.
    /// </summary>
    /// <remarks>
    /// <c>rr:lasting-effects.1</c> keeps the effect after the creating ability
    /// resolves. Its two bounds are independent: playing a card spends its one
    /// use, while <c>rr:lasting-effects.5</c> expires it at the end of the
    /// player phase if no card was played. The affected identity names the
    /// player without introducing another seat-shaped field on
    /// <see cref="ContinuousEffect"/>.
    /// </remarks>

    /// <summary>
    /// Plays a card — <c>rr:initiating-abilities</c> steps 1 to 7.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="seat">Who is playing it.</param>
    /// <param name="card">The card.</param>
    /// <param name="paying">The cards discarded to pay, by object id.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="targets">The chosen attachment host, when the card names one.</param>
    /// <exception cref="RulesNotImplementedException">
    /// A restriction is not met, the payment does not cover the cost, or the
    /// card needs a rule this engine does not have.
    /// </exception>
    public static void Play(
        World world, ICardFacts facts, ICardPlayAbilities abilities, Seat seat, Card card,
        IReadOnlyList<int> paying, List<GameEvent> events,
        IReadOnlyList<int>? targets = null)
        => PlayAllocated(world, facts, abilities, seat, card,
            new Decision(card.ObjectId, targets ?? [], paying), events);

    /// <summary>Plays a card with its exact declared resource allocation.</summary>
    public static void PlayAllocated(
        World world, ICardFacts facts, ICardPlayAbilities abilities, Seat seat,
        Card card, Decision choice, List<GameEvent> events)
        => PlayScoped(world, new ScopedCardPlayRequest(facts, abilities, seat, card, choice), events);

    /// <summary>Plays a card with a reduction supplied by the resolving effect.</summary>
    public static void PlayScoped(World world, ScopedCardPlayRequest request, List<GameEvent> events) =>
        ScopedPermanentPlay.Play(world, request, events);

    /// <summary>Plays a card while an effect ignores its resource cost.</summary>
    /// <remarks>
    /// This is a distinct entry point because ignoring a cost is a permission
    /// supplied by another resolving ability, not a payment choice on the
    /// card's ordinary affordance. All play restrictions still apply.
    /// </remarks>
    public static void PlayIgnoringResourceCost(
        World world, ICardFacts facts, ICardPlayAbilities abilities, Seat seat, Card card,
        List<GameEvent> events, IReadOnlyList<int>? targets = null) =>
        FreeCardPlay.Play(world, facts, abilities, seat, card, events, targets);

    /// <summary>Plays an owned card from an out-of-play zone by permission.</summary>
    /// <remarks>
    /// <c>rr:play-restrictions-and-permissions.2</c> allows a permission to
    /// override the normal timing or source-zone specification; its example is
    /// an ally played from a discard pile. The caller is the resolving effect
    /// that supplied that permission. Printed restrictions and the ordinary
    /// resource cost remain in force.
    /// </remarks>
    public static void PlayWithPermission(
        World world, ICardFacts facts, ICardPlayAbilities abilities, Seat seat, Card card,
        IReadOnlyList<int> paying, List<GameEvent> events,
        IReadOnlyList<int>? targets = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(paying);
        ArgumentNullException.ThrowIfNull(events);

        if (EffectiveCards.Kind(card, facts) == CardKind.Event)
        {
            throw new RulesNotImplementedException(
                $"event '{card.FaceId}' cannot use an out-of-zone play permission "
                + "until its Action selection can be represented");
        }

        if (!Permitted(
                world, facts, seat, card, targets, abilities,
                outOfPlayPermission: true))
        {
            throw new RulesNotImplementedException(
                $"card {card.ObjectId} ('{card.FaceId}') cannot use this play permission");
        }

        var adjusted = CardPayment.CostOf(world, facts, seat, card, targets is { Count: 1 } ? targets[0] : null);
        CardPlayPayment.Play(world,
            new CardPlayInitiation(facts, abilities, seat, card, adjusted, [.. targets ?? []], [.. paying]), events);
    }

    /// <summary>
    /// Puts an ally into play under the named player's control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:play-put-into-play</c> ignores the ally's resource cost and the
    /// ordinary restrictions on playing it, then places it in its controller's
    /// play area. Ordinarily the card's <see cref="Card.Owner"/> does not
    /// change; if it later leaves play, <c>rr:ownership-and-control.7.2</c>
    /// sends it to its owner's corresponding out-of-play area. The scenario
    /// player-card exception in <c>rr:ownership-and-control.2.2</c> instead
    /// makes its entering controller its owner.
    /// </para>
    /// <para>
    /// <c>rr:play-put-into-play.3</c> says this is not playing the card, so no
    /// <c>CardPlayed</c> step is scheduled. It still enters play, and therefore
    /// uses the same entry lifecycle as a card played from hand.
    /// </para>
    /// </remarks>
    public static void PutAllyIntoPlay(
        World world, ICardFacts facts, ICardPlayAbilities abilities, Card ally,
        int controller, string trigger, List<GameEvent> events) =>
        AllyCardEntry.Put(world, facts, abilities, new AllyEntryRequest(ally, controller, trigger), events);

    /// <summary>
    /// Spends resources to pay a cost — <c>rr:initiating-abilities.step.5</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "If this step is reached and the cost(s) cannot be paid, <b>abort this
    /// process without paying any costs</b>", so the whole payment is checked
    /// before a single card is discarded.
    /// </para>
    /// <para>
    /// Shared by a card being played and an ability being triggered, because
    /// <c>rr:cost</c> is one rule for both — "a cost is anything a player must
    /// do or pay in order to initiate an ability", and playing a card is
    /// initiating one.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="hands">Whose hands may pay — see <see cref="Paying"/>.</param>
    /// <param name="paying">The cards discarded to pay, by object id.</param>
    /// <param name="cost">How many resources are needed.</param>
    /// <param name="required">Specific types that must be among them, or empty.</param>
    /// <param name="itself">
    /// A card that cannot pay for itself, or <c>-1</c>. <c>rr:cost.3</c> spends
    /// resources "by discarding cards from their hand", and a card leaving the
    /// hand to be played is not also in it.
    /// </param>
    /// <param name="payer">
    /// Whose cost it is, for a generator that is an ability rather than a card.
    /// </param>
    /// <param name="events">Where to record what moved.</param>
    /// <param name="payingFor">The card being paid for, or null for an ability cost.</param>
    /// <param name="resourcePayers">
    /// Which player owns each selected resource ability. Ordinary payments
    /// omit this because every ability belongs to <paramref name="payer"/>;
    /// alliance payments may name a helper's ability.
    /// </param>
    public static ResourcePaymentReceipt Spend(
        World world, ICardFacts facts, IReadOnlyList<Area> hands, IReadOnlyList<int> paying,
        long cost, string required, int itself, int payer, List<GameEvent> events,
        Card? payingFor = null, IReadOnlyDictionary<int, int>? resourcePayers = null)
        => Spend(world, facts, world.ResourceAbilities, hands, paying, cost, required,
            itself, payer, events, payingFor, resourcePayers);

    /// <summary>Spends resources through one explicit card capability.</summary>
    public static ResourcePaymentReceipt Spend(
        World world, ICardFacts facts, IResourceCardAbilities resourceAbilities,
        IReadOnlyList<Area> hands, IReadOnlyList<int> paying,
        long cost, string required, int itself, int payer, List<GameEvent> events,
        Card? payingFor = null, IReadOnlyDictionary<int, int>? resourcePayers = null) =>
        new CardResourcePayment(world, facts, resourceAbilities, new CardResourcePaymentRequest
        {
            Hands = [.. hands], Paying = [.. paying], Cost = cost, Required = required, Itself = itself,
            Payer = payer, PayingFor = payingFor, ResourcePayers = resourcePayers?.ToDictionary(pair => pair.Key, pair => pair.Value),
        }).Spend(events);

}
