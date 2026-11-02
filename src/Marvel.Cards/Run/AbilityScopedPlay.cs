using Marvel.Rules.Play;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// The choice routes each supported card kind through its ordinary play lifecycle.
// Three scopes share it: a shared-trait card at a cost reduced by 1, any card
// played ignoring its resource cost, and any card played at a stated reduction.
internal static class AbilityScopedPlay
{
    /// <summary>The resolution result naming the played card's printed resource cost.</summary>
    internal const string PrintedCostPlayed = "printedCostPlayed";

    internal static bool IsScopedPlay(AbilityEffect effect) => effect is AbilityCardPlayEffect.PlayFromHand or AbilityEffect.Fixed
    { Instruction: AbilityFixedInstruction.PlaySharedHeroTraitCard or AbilityFixedInstruction.PlayIgnoringResourceCost };

    // One scoped play's terms, read from its instruction.
    private sealed record Scope(bool SharesHeroTrait, bool Ignores, long Reduction, bool Optional);

    private static Scope Of(AbilityEffect choice) => choice switch
    {
        AbilityCardPlayEffect.PlayFromHand play => new(false, false, play.Reduction, play.Optional),
        AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.PlayIgnoringResourceCost } => new(false, true, 0, false),
        _ => new(true, false, 1, false),
    };

    private static bool Ignores(AbilityEffect choice) => Of(choice).Ignores;

    /// <summary>Commits the accepted play and suspends until it completes; false for an accepted decline.</summary>
    internal static bool Commit(AbilityResolutionExecution execution, AbilityResolutionState cast,
        AbilityEffect choice, Decision input)
    {
        var context = execution.StructuralContext(cast);
        var request = Validate(context, choice, input);
        // rr:may: an optional play the player declines resolves nothing.
        if (request is null) return false;
        if (Ignores(choice))
            cast.Results[PrintedCostPlayed] = Resources.Cost(request.Card.FaceId, cast.World.Facts, cast.World.Players) ?? 0;
        var owner = cast.World.Agenda.Current;
        var occurrence = cast.World.Agenda.Occurrence;
        if (EffectiveCards.Kind(request.Card, cast.World.Facts) == CardKind.Event)
        {
            var pending = AbilityScopedEvent.Action(context.Program, context.ResourceAbilities, cast.World, request.Card,
                request.Reduction!, input.Targets);
            var occurrenceForEvent = new Occurrence(0, [Steps.TurnAction],
                Subject: request.Card.ObjectId, Player: cast.Player);
            var result = cast.World.Agenda.Executions.Run(occurrenceForEvent, pending,
                invocation => execution.Act(cast.World, invocation, input, request.Reduction));
            cast.Events.AddRange(result.Events);
        }
        else CardPlay.PlayScoped(cast.World, request, cast.Events);
        // The continuation settles this outcome after the child play completes.
        cast.CompletedOutcome = AbilityStructuralOutcome.Full;
        execution.SuspendAfterProcedure(choice, cast, owner, occurrence);
        return true;
    }

    internal static Prompt Describe(AbilityStructuralContext context, AbilityEffect choice) =>
        Describe(new Quote(context.Expressions.World, context.Player, context.Expressions.Source, false,
            context.Program, context.ResourceAbilities, Of(choice)));

    internal static bool CanInitiate(AbilityAdmissionContext context, AbilityEffect node) =>
        Describe(new Quote(context.World, context.Query.Player, context.Source, true,
            context.Program, context.ResourceAbilities, Of(node))).Affordances.Count > 0;

    // One scoped-play prompt's fixed inputs.
    private sealed record Quote(World World, int Player, Card Source, bool ExcludeSource,
        AbilityProgram Program, IResourceCardAbilities Resources, Scope Scope)
    {
        internal bool Ignores => Scope.Ignores;
    }

    private static Prompt Describe(Quote quote)
    {
        var (world, player, source, _, _, _, scope) = quote;
        var seat = world.Seats[player];
        var offers = new List<Affordance>();
        // A shared trait is read from the hero; any other scope plays in either form,
        // where each card's own form requirement still applies (rr:play-put-into-play.1).
        if (!scope.SharesHeroTrait || Forms.In(world, seat, world.Facts, "hero"))
        {
            var heroTraits = Traits.Of(world, seat.IdentityCard, world.Facts);
            foreach (var card in seat.Hand.Cards)
            {
                if (!Eligible(quote, card, heroTraits)) continue;
                var reduction = Reduction(world, seat, card, scope);
                var offer = EffectiveCards.Kind(card, world.Facts) == CardKind.Event
                    ? AbilityScopedEvent.Offer(world, card, source, quote.Program, quote.Resources, quote.ExcludeSource, reduction)
                    : PermanentOffer(quote, card, reduction);
                if (offer is not null) offers.Add(offer);
            }
        }
        var prompt = new Prompt(player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{source.FaceId}: choose a card to play", scope.Optional, offers)
        {
            DisplayQuestion = DisplayQuestion(scope),
            Description = Description(world, source, scope),
            ContextCardIds = [source.ObjectId],
        };
        return scope.Optional ? prompt with { DeclineLabel = "Do not play a card" } : prompt;
    }

    private static string DisplayQuestion(Scope scope) =>
        scope.Ignores ? "Choose a card to play ignoring its resource cost"
        : scope.SharesHeroTrait ? "Choose a matching card to play"
        : $"Choose a card to play for {scope.Reduction} less";

    private static string Description(World world, Card source, Scope scope) =>
        scope.Ignores
            ? "Play a card from your hand ignoring its resource cost. Then discard cards from the top of the encounter deck equal to that card's printed resource cost."
            : scope.SharesHeroTrait
            ? $"{EffectiveCards.Title(source, world.Facts)} was exhausted. Play a card from your hand that shares a trait with your hero; reduce its cost by 1. The shown payment is the current cost."
            : $"{EffectiveCards.Title(source, world.Facts)}: {(scope.Optional ? "you may play" : "play")} a card from your hand, reducing its resource cost by {scope.Reduction}. The shown payment is the current cost.";

    private static bool Eligible(Quote quote, Card card, IEnumerable<string> heroTraits) =>
        quote.Ignores ? CanIgnoreCost(quote.World, card, quote.Source)
        : !quote.Scope.SharesHeroTrait || Traits.Of(quote.World, card, quote.World.Facts).Intersect(heroTraits).Any();

    private static Affordance? PermanentOffer(Quote quote, Card card, ScopedCardCostReduction reduction)
    {
        var world = quote.World;
        var seat = world.Seats[quote.Player];
        var hosts = CardPlayLegality.LegalAttachmentTargets(world, world.Facts, seat, card, world.CardPlayAbilities);
        var prices = hosts is null
            ? new[] { CardPayment.Price(world, world.Facts, seat, card, null, reduction) }.OfType<CostOption>().ToArray()
            : hosts.Select(host => CardPayment.Price(world, world.Facts, seat, card, host, reduction))
                .OfType<CostOption>().ToArray();
        if (quote.ExcludeSource)
            prices = prices.Where(price => CanPayWithoutSource(world, card, price, quote.Source)).ToArray();
        if (prices.Length == 0) return null;
        return new Affordance(card.ObjectId, AbilityStructuralExecution.ChooseVerb,
            card.ObjectId, seat.Index, card.FaceId,
            Targets: hosts is null ? null : new TargetRequest(prices.Select(price => price.Target).ToArray(), 1, 1),
            Costs: prices, Description: PlayDescription(world, quote.Program, card, quote.Source, quote.Scope.Reduction))
        { PlaysCard = true, SelectionSourceId = quote.Source.ObjectId };
    }

    private static ScopedCardCostReduction Reduction(World world, Seat seat, Card card, Scope scope) =>
        scope.Ignores ? ScopedCardCostReduction.IgnoringResourceCost(world, seat, card)
        : new ScopedCardCostReduction(world, seat, card, scope.Reduction);

    // rr:requirement-resources.2: "A card with the requirement keyword cannot be
    // played 'ignoring its resource cost'". The scoped play's payment check also
    // refuses such a card, so the requirement test here is an equivalent mutant
    // kept to state the rule. Chaos Magic is still in hand while its offer is
    // admitted, so it excludes itself.
    private static bool CanIgnoreCost(World world, Card card, Card source) =>
        !ReferenceEquals(card, source) && Resources.HasPlayableCost(card.FaceId, world.Facts)
        && Resources.Required(world, card, world.Facts).Length == 0;

    // The engine price already applies every modifier, so the copy only names
    // this source's share and the floor that can absorb it.
    // A reduction of 0 names a play ignoring its resource cost.
    internal static string PlayDescription(World world, AbilityProgram program, Card card, Card source, long reduction) =>
        (reduction == 0
            ? $"Played ignoring its resource cost; then discard {Resources.Cost(card.FaceId, world.Facts, world.Players) ?? 0} encounter cards for its printed cost."
            : $"This price already includes {EffectiveCards.Title(source, world.Facts)}'s reduction of {reduction} (a cost can't go below 0).")
        + (AbilityEntryCounterDescription.PlaySummary(world, program, card) is { } entry ? $" {entry}" : string.Empty);

    private static bool CanPayWithoutSource(World world, Card card, CostOption price, Card source)
    {
        var generators = price.Generators.Where(candidate => candidate.Effect != source.ObjectId).ToArray();
        var required = price with { Rule = [Resources.Required(world, card, world.Facts)], Sources = generators };
        return ResourcePayment.Allocate(required, generators.Select(generator => generator.Effect).ToArray()) is not null;
    }

    /// <summary>The accepted play, or null for an accepted decline.</summary>
    internal static ScopedCardPlayRequest? Validate(AbilityStructuralContext context, AbilityEffect choice, Decision input)
    {
        var prompt = Describe(context, choice);
        var assessment = DecisionAssessment.Assess(prompt, input);
        if (!assessment.IsAccepted)
            throw new ArgumentException(assessment.Reason ?? assessment.Rejection.ToString(), nameof(input));
        if (input.IsDecline) return null;
        var world = context.Expressions.World;
        var seat = world.Seats[context.Player];
        var card = world.Cards[input.Affordance];
        var request = new ScopedCardPlayRequest(world.Facts, world.CardPlayAbilities, seat, card, input,
            Reduction(world, seat, card, Of(choice)));
        if (EffectiveCards.Kind(card, world.Facts) == CardKind.Event)
            AbilityScopedEvent.Validate(context, card, input, request.Reduction!);
        else ScopedPermanentPlay.Validate(world, request);
        return request;
    }
}
