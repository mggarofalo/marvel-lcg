using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Read-only action and resource discovery over a checked program. It builds
// immutable admission contexts and never records a use or resolves an effect.
internal sealed class AbilityOfferQueries
{
    private readonly AbilityProgram program;
    private readonly IResourceCardAbilities resourceAbilities;

    internal AbilityOfferQueries(
        AbilityProgram program, IResourceCardAbilities resourceAbilities)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(resourceAbilities);
        this.program = program;
        this.resourceAbilities = resourceAbilities;
    }

    internal IReadOnlyList<PendingAbility> Actions(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);

        var found = new List<PendingAbility>();
        foreach (var card in Triggerable(world, player).ToList())
        {
            var written = AbilityProgramQueries.On(program, card);
            for (int index = 0; index < written.Count; index++)
            {
                var ability = written[index];
                if (ability.Trigger.Timing is not (AbilityType.Action or AbilityType.ForcedAction)
                    || !AbilityPaymentRules.Payable(
                        world, card, player, ability.Cost, program, resourceAbilities)
                    || !AbilityPaymentRules.EventPayable(
                        world, card, player, ability, resourceAbilities)
                    || !ActionAvailable(world, card, ability, player, occurrence: null))
                {
                    continue;
                }

                int ordinal = written.Take(index).Count(candidate =>
                    candidate.Trigger.Timing == ability.Trigger.Timing);
                found.Add(new PendingAbility(card.ObjectId, ability.Trigger.Timing, player, ordinal));
            }
        }
        return found;
    }

    internal bool ActionAvailable(
        World world, Card card, CompiledCardAbility ability, int player, Occurrence? occurrence) =>
        ActionAdmission(world, card, ability, player, occurrence) is not null;

    internal AbilityAdmissionResult? ActionAdmission(
        World world, Card card, CompiledCardAbility ability, int player, Occurrence? occurrence)
    {
        int index = AbilityAvailability.IndexOf(program, card, ability);
        if (!MayInitiate(world, ability, card, player)
            || !AbilityAvailability.Available(world, card, ability, index, occurrence)
            || !InForm(world, player, ability.Trigger.Form)) return null;

        var context = TurnAction(world, card, player, occurrence, ability.Cost);
        if (ability.When is { } condition && !context.Evaluator().Test(condition)) return null;
        context = context.WithReachability(context.Reachability with { CheckingInitiation = true });
        if (!AbilityInitiation.LabelsCanInitiate(ability, context)) return null;
        if (ability.Labels.Length > 0 && LabeledAbilities.WouldBeCancelled(
                world, world.Facts, AbilityCardQueries.Resolver(context.Query), card, ability.Labels))
        {
            return new AbilityAdmissionResult(true, []);
        }

        var result = AbilityInitiation.Admit(ability.Effect, context);
        return result.IsAdmissible ? result : null;
    }

    internal IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);

        var sources = new List<ResourceSource>();
        foreach (var card in Triggerable(world, player).ToList())
        {
            if (ResourceAbility(world, player, card) is not { } ability) continue;
            string generated = Generated(ability.Effect, world, player);
            if (generated.Length > 0) sources.Add(new ResourceSource(card.ObjectId, generated));
        }
        return sources;
    }

    internal string ResourceGeneratorName(World world, int player, int card)
    {
        ArgumentNullException.ThrowIfNull(world);
        Card source = world.Cards[card];
        if (source.Area.Type == DeckType.HandsArea) return world.Facts.Title(source.FaceId);

        return ResourceAbility(world, player, source)?.Name ?? world.Facts.Title(source.FaceId);
    }

    internal IReadOnlyList<ResourceSource> PrintedResourceAbilities(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        return [.. ResourceAbilities(world, player).Where(source =>
            AbilityProgramQueries.On(program, world.Cards[source.Effect]).Any(ability =>
                ability.Trigger.Timing == AbilityType.Resource
                && string.Equals(ability.PrintedResources, source.Generates, StringComparison.Ordinal)))];
    }

    internal CompiledCardAbility? ResourceAbility(World world, int player, Card card)
    {
        var written = AbilityProgramQueries.On(program, card);
        for (int index = 0; index < written.Count; index++)
        {
            var ability = written[index];
            if (ability.Trigger.Timing != AbilityType.Resource
                || !MayInitiate(world, ability, card, player)
                || !AbilityAvailability.Available(world, card, ability, index)
                || !InForm(world, player, ability.Trigger.Form)
                || !AbilityPaymentRules.Payable(
                    world, card, player, ability.Cost, program, resourceAbilities)) continue;

            var context = TurnAction(world, card, player, occurrence: null, ability.Cost);
            if (ability.When is null || context.Evaluator().Test(ability.When)) return ability;
        }
        return null;
    }

    internal static string Generated(AbilityEffect effect, World world, int player) => effect switch
    {
        AbilityEffect.Generate generate => generate.Resources,
        AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.GenerateTopDiscard } =>
            world.AreaOf(DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player).Cards is { Count: > 0 } cards
                ? Resources.GeneratedBy(cards[^1].FaceId, world.Facts)
                : string.Empty,
        _ => throw new RulesNotImplementedException(
            $"a resource ability whose compiled effect is '{effect.GetType().Name}' generates nothing this engine can read"),
    };

    private IEnumerable<Card> Triggerable(World world, int player)
    {
        foreach (var area in world.Areas.Where(area => DeckTypes.IsInPlay(area.Type)))
        foreach (var card in area.Cards)
            if (AbilityCardQueries.ControllerOf(world, card) == player
                || card.Owner == World.Scenario
                || AbilityProgramQueries.On(program, card).Any(ability => ability.AnyPlayer))
                yield return card;

        foreach (var card in world.Seats[player].Hand.Cards)
            if (world.Facts.Kind(card.FaceId) == CardKind.Event) yield return card;
    }

    private AbilityAdmissionContext TurnAction(
        World world, Card card, int player, Occurrence? occurrence = null, AbilityCost? cost = null)
    {
        occurrence ??= new Occurrence(
            0, [Steps.TurnAction], Subject: card.ObjectId, Player: player);
        var query = new AbilityQueryContext(
            world, card, occurrence, player, card.Incarnation, null, null, null, []);
        return new AbilityAdmissionContext(
            program, resourceAbilities,
            new AbilityExpressionContext(
                query, ImmutableDictionary<string, long>.Empty, [], string.Empty,
                -1, false, null),
            new AbilityReachabilityContext
            {
                PaymentMayMutate = cost is not null || world.Facts.Kind(card.FaceId) == CardKind.Event,
                PaymentCost = cost,
            },
            Power: null);
    }

    private static bool InForm(World world, int player, string? form) =>
        form is null || Forms.In(world, world.Seats[player], world.Facts, form);

    private bool MayInitiate(World world, CompiledCardAbility ability, Card card, int player)
    {
        bool cardPermits = AbilityCardQueries.ControllerOf(world, card) == player
            || card.Owner == World.Scenario || ability.AnyPlayer;
        return cardPermits && (RestrictedPlayer(world, ability, card) is not { } restricted
            || restricted == player);
    }

    private int? RestrictedPlayer(World world, CompiledCardAbility ability, Card card)
    {
        if (ability.AnyPlayer) return null;
        if (world.Facts.Kind(card.FaceId) == CardKind.Obligation && card.Area.PlayArea.IsPlayers)
            return card.Area.PlayArea.Player;
        if (world.Facts.Kind(card.FaceId) == CardKind.Attachment
            && card.Area.Host >= 0 && card.Area.Host < world.Cards.Count
            && AbilityPlayerBindingAnalysis.UsesYouOrYour(program, ability, card))
        {
            int controller = AbilityCardQueries.ControllerOf(world, world.Cards[card.Area.Host]);
            return controller >= 0 ? controller : null;
        }
        return null;
    }
}
