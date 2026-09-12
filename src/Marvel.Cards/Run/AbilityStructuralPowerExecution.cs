using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;

using static Marvel.Cards.Run.AbilityStructuralExecution;
using static Marvel.Cards.Run.AbilityStructuralPowerExecution;
using static Marvel.Cards.Run.AbilityStructuralFlowExecution;
namespace Marvel.Cards.Run;

// One immutable read of a live resolution. The executor refreshes it after every
// command, so structural decisions never retain a stale view of the board.

/// <summary>Owns typed, deterministic traversal of structural effect nodes.</summary>
internal static class AbilityStructuralPowerExecution
{
    internal static AbilityStructuralTransition Power(
        AbilityStructuralContext context, AbilityEffect.Power power,
        Card? target = null, IReadOnlyList<Card>? targets = null, long amount = -1)
    {
        string verb = power.Kind switch
        {
            AbilityPowerKind.Attack => BasicPowers.AttackVerb,
            AbilityPowerKind.Thwart => BasicPowers.ThwartVerb,
            _ => throw new InvalidOperationException("A defense power is not scheduled"),
        };
        target ??= power.Target is { } selector ? Find(selector, context) : null;
        if (target is null)
            return new Unsupported($"'{context.SourceFace}' cannot find the target of its {verb}");

        var selected = context.Admission().WithSelection(target);
        if (AbilityPowerTrace.SuspendsPowerEffect(
            power.Effect, selected, bindingMayChange: amount >= 0))
        {
            return new Unsupported(
                $"'{context.SourceFace}' suspends inside a {verb.ToLowerInvariant()}, which is not implemented");
        }

        var abilities = Abilities(context);
        var addresses = abilities
            .Select((ability, index) => (Ability: ability, Index: index))
            .Where(candidate => context.Tier is null
                || candidate.Ability.Trigger.Timing == context.Tier)
            .SelectMany(candidate => AbilityAdmission.PowerEffects(candidate.Ability.Effect, verb)
                .Select((wrapper, ordinal) =>
                    (candidate.Index, Ordinal: ordinal, Wrapper: wrapper)))
            .Where(candidate => ReferenceEquals(candidate.Wrapper, power))
            .ToList();
        if (addresses.Count != 1)
        {
            return new Unsupported(
                $"'{context.SourceFace}' {verb.ToLowerInvariant()} has {addresses.Count} reconstructable authored locations");
        }

        var address = addresses[0];
        bool automatic = power.AutomaticTarget
            || context.CrisisIgnoringThwarts.Contains(power)
            || context.PersistedCrisisIgnoringThwarts.Contains(address.Ordinal);
        return new SchedulePowerCommand(
            power, verb, target, [.. targets ?? [target]], amount,
            address.Index, address.Ordinal, automatic);
    }

    internal static AbilityStructuralTransition ThwartAll(
        AbilityStructuralContext context, AbilityEffect.ThwartGroup group)
    {
        var schemes = Every(group.Schemes, context);
        return schemes.Count == 0
            ? new Complete(context.Frames)
            : Power(context, group.Thwart, schemes[0], schemes);
    }

    internal static AbilityStructuralTransition Activation(
        AbilityStructuralContext context, AbilityEffect.ActivateEnemies activation,
        IReadOnlyList<Card>? ordered = null)
    {
        Card? against = activation.Against is { } named ? Find(named, context) : null;
        int seat = ActivationSeat(context, activation);
        if (seat < 0)
            return new Unsupported(
                $"'{context.SourceFace}' initiates an enemy attack against a character with no attacked player");

        var enemies = ordered ?? ActivationCandidates(context, activation);
        if (enemies.Count > 1 && ordered is null)
            return AskFor(context, activation);

        var targets = ActivationTargets(context, activation, enemies, seat);
        return new ScheduleActivationsCommand(
            activation, targets, against?.ObjectId ?? -1,
            activation.First, activation.Dynamic);
    }

    private static int ActivationSeat(
        AbilityStructuralContext context, AbilityEffect.ActivateEnemies activation) =>
        activation.Against switch
        {
            AbilityCardSelection.Bound { Binding: AbilityCardBinding.TriggerActor } =>
                context.Expressions.Occurrence.ActorFacts?.Controller ?? World.Scenario,
            AbilityCardSelection.Bound { Binding: AbilityCardBinding.TriggerTarget } =>
                context.Expressions.Occurrence.TargetFacts?.Controller ?? World.Scenario,
            _ => context.Player,
        };

    private static ImmutableArray<ActivationTarget> ActivationTargets(
        AbilityStructuralContext context, AbilityEffect.ActivateEnemies activation,
        IReadOnlyList<Card> enemies, int seat) =>
        [
            .. enemies.Select(enemy => new ActivationTarget(
                    enemy, activation.EngagedHero ? enemy.Area.PlayArea.Player : seat))
                .Where(target => target.Seat >= 0
                    && (!activation.EngagedHero || Forms.In(
                        context.Expressions.World,
                        context.Expressions.World.Seats[target.Seat],
                        context.Expressions.World.Facts, Forms.Hero))),
        ];

    internal static List<Card> ActivationCandidates(
        AbilityStructuralContext context, AbilityEffect.ActivateEnemies activation) =>
        Every(activation.Enemies, context).Where(enemy => !activation.Dynamic
            || context.Expressions.Results.GetValueOrDefault($"dynamicActivation:{enemy.ObjectId}") == 0).ToList();

    internal static ImmutableArray<CompiledCardAbility> Abilities(
        AbilityStructuralContext context) =>
        string.Equals(context.AbilityFace, context.Expressions.Source.FaceId,
            StringComparison.Ordinal)
            ? [.. AbilityProgramQueries.On(context.Program, context.Expressions.Source)]
            : context.Program.On(context.AbilityFace);

    internal static AbilityDamageAndThreatContext DamageContext(AbilityStructuralContext context) =>
        new(context.Expressions, context.Program, context.Trigger, [], context.AbilityActor, null, context.Power,
            context.HasContinuation, null, null, 0, context.ThreatAbilities);

    internal static Prompt DescribeSpecials(AbilityStructuralContext context, AbilityEffect.CardAction specials)
    {
        var cards = Every(specials.Selection, context);
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed,
            Steps.ResolveSpecial, $"{context.SourceFace}: order Special abilities", false,
            [new Affordance(context.Expressions.Source.ObjectId, ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, specials.OperationName(),
                new TargetRequest([.. cards.Select(card => card.ObjectId)], cards.Count, cards.Count))]);
    }

    internal static AbilityStructuralTransition AnswerSpecials(
        AbilityStructuralContext context, AbilityEffect.CardAction specials, Decision answer)
    {
        var legal = Every(specials.Selection, context).Select(card => card.ObjectId).ToHashSet();
        return answer.IsDecline || answer.Targets.Count != legal.Count
            || answer.Targets.Distinct().Count() != legal.Count
            || answer.Targets.Any(id => !legal.Contains(id))
            ? new Unsupported($"'{context.SourceFace}' requires one permutation of all {legal.Count} Special abilities")
            : new ResolveSpecialsCommand([.. answer.Targets]);
    }

    internal static Prompt DescribeTopForHand(AbilityStructuralContext context, AbilityEffect.ChooseTopForHand top)
    {
        var cards = TopCards(context.Expressions.World.Seats[context.Player].Deck, top.Count);
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: choose a top card", false,
            cards.Select(card => new Affordance(card.ObjectId, ChooseVerb,
                card.ObjectId, context.Player, card.FaceId)).ToList())
        { ExposesConcealedCandidates = true };
    }

    internal static AbilityStructuralTransition AnswerTopForHand(
        AbilityStructuralContext context, AbilityEffect.ChooseTopForHand top, Decision answer)
    {
        var cards = TopCards(context.Expressions.World.Seats[context.Player].Deck, top.Count);
        return answer.IsDecline || cards.All(card => card.ObjectId != answer.Affordance)
            ? new Unsupported($"'{context.SourceFace}' did not offer card {answer.Affordance} among its top cards")
            : new ChooseTopForHandCommand(answer.Affordance, [.. cards.Select(card => card.ObjectId)]);
    }

    internal static Prompt DescribeDiscardShuffle(
        AbilityStructuralContext context, AbilityEffect.ChooseDiscardToShuffle discard)
    {
        var area = context.Expressions.World.AreaOf(DeckType.DiscardPile,
            PlayArea.Of(context.Player), cardOwner: context.Player);
        int maximum = Math.Min(discard.Maximum, area.Cards.Select(card =>
            context.Expressions.World.Facts.Title(card.FaceId)).Distinct().Count());
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: choose cards to shuffle", false,
            [new Affordance(context.Expressions.Source.ObjectId, ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, discard.OperationName(),
                new TargetRequest([.. area.Cards.Select(card => card.ObjectId)], 1, maximum))]);
    }

    internal static AbilityStructuralTransition AnswerDiscardShuffle(
        AbilityStructuralContext context, AbilityEffect.ChooseDiscardToShuffle discard, Decision answer)
    {
        var area = context.Expressions.World.AreaOf(DeckType.DiscardPile,
            PlayArea.Of(context.Player), cardOwner: context.Player);
        var cards = answer.Targets.Select(id => area.Cards.FirstOrDefault(card => card.ObjectId == id)).ToList();
        if (answer.IsDecline || cards.Any(card => card is null)
            || cards.Count < 1 || cards.Count > discard.Maximum
            || cards.Select(card => context.Expressions.World.Facts.Title(card!.FaceId)).Distinct().Count() != cards.Count)
        {
            return new Unsupported($"'{context.SourceFace}' requires one to {discard.Maximum} cards with different titles");
        }
        return new ShuffleDiscardCommand([.. answer.Targets]);
    }

}
