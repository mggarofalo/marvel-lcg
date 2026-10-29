using Marvel.Rules.Play;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// This choice is limited to ordinary permanent plays. Event actions require their own lifecycle.
internal static class AbilitySharedTraitPlay
{
    internal static List<GameEvent> Commit(AbilityResolutionExecution execution, AbilityResolutionState cast,
        AbilityEffect choice, Decision input)
    {
        var request = Validate(execution.StructuralContext(cast), input);
        var owner = cast.World.Agenda.Current;
        var occurrence = cast.World.Agenda.Occurrence;
        CardPlay.PlayScoped(cast.World, request, cast.Events);
        // The continuation settles this outcome after the child play completes.
        cast.CompletedOutcome = AbilityStructuralOutcome.Full;
        execution.SuspendAfterProcedure(choice, cast, owner, occurrence);
        return cast.Events;
    }

    internal static Prompt Describe(AbilityStructuralContext context) =>
        Describe(context.Expressions.World, context.Player, context.Expressions.Source, false);

    internal static bool CanInitiate(World world, int player, Card source) =>
        Describe(world, player, source, true).Affordances.Count > 0;

    private static Prompt Describe(World world, int player, Card source, bool excludeSource)
    {
        var seat = world.Seats[player];
        var offers = new List<Affordance>();
        if (Forms.In(world, seat, world.Facts, "hero"))
        {
            var heroTraits = Traits.Of(world, seat.IdentityCard, world.Facts);
            foreach (var card in seat.Hand.Cards)
            {
                if (!Traits.Of(world, card, world.Facts).Intersect(heroTraits).Any()) continue;
                var reduction = new ScopedCardCostReduction(world, seat, card, 1);
                var hosts = CardPlayLegality.LegalAttachmentTargets(world, world.Facts, seat, card, world.CardPlayAbilities);
                var prices = hosts is null
                    ? new[] { CardPayment.Price(world, world.Facts, seat, card, null, reduction) }.OfType<CostOption>().ToArray()
                    : hosts.Select(host => CardPayment.Price(world, world.Facts, seat, card, host, reduction))
                        .OfType<CostOption>().ToArray();
                if (excludeSource)
                    prices = prices.Where(price => CanPayWithoutSource(world, card, price, source)).ToArray();
                if (prices.Length == 0) continue;
                offers.Add(new Affordance(card.ObjectId, AbilityStructuralExecution.ChooseVerb,
                    card.ObjectId, seat.Index, card.FaceId,
                    Targets: hosts is null ? null : new TargetRequest(prices.Select(price => price.Target).ToArray(), 1, 1),
                    Costs: prices) { PlaysCard = true, SelectionSourceId = source.ObjectId });
            }
        }
        return new Prompt(player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{source.FaceId}: choose a card to play", false, offers);
    }

    private static bool CanPayWithoutSource(World world, Card card, CostOption price, Card source)
    {
        var generators = price.Generators.Where(candidate => candidate.Effect != source.ObjectId).ToArray();
        var required = price with { Rule = [Resources.Required(world, card, world.Facts)], Sources = generators };
        return ResourcePayment.Allocate(required, generators.Select(generator => generator.Effect).ToArray()) is not null;
    }

    internal static ScopedCardPlayRequest Validate(AbilityStructuralContext context, Decision input)
    {
        var prompt = Describe(context);
        var assessment = DecisionAssessment.Assess(prompt, input);
        if (!assessment.IsAccepted)
            throw new ArgumentException(assessment.Reason ?? assessment.Rejection.ToString(), nameof(input));
        var world = context.Expressions.World;
        var seat = world.Seats[context.Player];
        var card = world.Cards[input.Affordance];
        var request = new ScopedCardPlayRequest(world.Facts, world.CardPlayAbilities, seat, card, input,
            new ScopedCardCostReduction(world, seat, card, 1));
        ScopedPermanentPlay.Validate(world, request);
        return request;
    }
}
