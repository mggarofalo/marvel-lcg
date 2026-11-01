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
internal static class AbilityStructuralExecution
{
    internal const string ChooseVerb = "Choose_Option";
    internal static bool EventMeansEffectApplied(AbilityEffect effect) =>
        effect.OperationName() is not (
            "seq" or "and" or "then" or "otherwise" or "eachPlayer" or "if"
            or "forEach" or "eachTime" or "choose" or "chooseCard"
            or "resolveSpecials" or "payOrExhaust" or "payOrEffect"
            or "chooseTopForHand" or "peekTop" or "inspectEncounterTop" or "inspectDiscardOrderEncounterTop" or "discardHandCards" or "discardResourceIcons" or "chooseStatusRecipients" or "chooseHealRecipients" or "chooseCardsToHand" or "chooseDiscardToShuffle" or "exhaustAnyNumber"
            or "distributeThreat" or "thwartDifferentSchemes" or "makeTheCall" or "playSharedHeroTraitCard" or "legalPractice"
            or "attack" or "defense" or "thwart" or "thwartSchemes"
            or "placeThreat" or "enemyAttacks" or "enemySchemes");

    internal static AbilityStructuralTransition Decide(AbilityStructuralContext context, AbilityEffect effect)
    {
        if (effect.OperationName() is "makeTheCall" or "playSharedHeroTraitCard" or "legalPractice" or "peekTop" or "inspectEncounterTop" or "inspectDiscardOrderEncounterTop")
            return AskFor(context, effect);
        return effect switch
        {
            AbilityEffect.ResolveInvocation => AbilityInvocationPayment.Resolve(context),
            AbilityEffect.Sequence sequence => new StartSequenceCommand(sequence),
            AbilityEffect.Simultaneous simultaneous => Simultaneous(context, simultaneous),
            AbilityEffect.Dependent dependent => new StartDependentCommand(dependent),
            AbilityEffect.Conditional conditional => Conditional(context, conditional),
            AbilityEffect.ForEach repeated => new StartForEachCommand(repeated),
            AbilityEffect.EachTime repeated => new StartEachTimeCommand(repeated),
            AbilityEffect.Choose choose => Choose(context, choose),
            AbilityEffect.ChooseCard choose => ChooseCard(context, choose),
            AbilityEffect.EachPlayer each => EachPlayer(context, each),
            AbilityEffect.CardAction { Instruction: AbilityCardInstruction.ResolveSpecials } specials =>
                AbilitySpecialSequence.Start(context, specials),
            AbilityEffect.PayOrEffect payment =>
                AbilityPaymentChoiceDescription.Describe(context, payment).Affordances.Count == 0
                    ? new Complete(context.Frames) : AskFor(context, payment),
            AbilityEffect.ChooseTopForHand top => ChooseTopForHand(context, top),
            AbilityEffect.DiscardHandCards discard => AbilitySelectedHandDiscard.Count(context, discard) > 0
                ? AskFor(context, discard) : new Complete(context.Frames),
            AbilityEffect.DiscardResourceIcons discard => AbilityResourceIconDiscard.Required(context, discard) > 0
                ? AskFor(context, discard) : new Complete(context.Frames),
            AbilityEffect.ChooseStatusRecipients group => AbilityStatusRecipients.Candidates(group, context.Admission()).Count > 0
                ? AskFor(context, group) : new Complete(context.Frames),
            AbilityRecipientEffect.ChooseHealRecipients group => AbilityHealRecipients.Candidates(group, context.Admission()).Count > 0
                ? AskFor(context, group) : new Complete(context.Frames),
            AbilityEffect.ChooseCardsToHand group => AskFor(context, group),
            AbilityEffect.ExhaustAnyNumber exhaust => AbilityAnyNumberExhaustion.Ask(context, exhaust),
            AbilityEffect.ChooseDiscardToShuffle discard => AskFor(context, discard),
            AbilityEffect.ThwartGroup group when group.Selection is AbilityThwartSelection.Different or AbilityThwartSelection.Distributed =>
                AskFor(context, group),
            AbilityEffect.AfterActivation after => AfterActivation(context, after),
            AbilityEffect.ActivateEnemies activation => Activation(context, activation),
            AbilityEffect.Power { Kind: AbilityPowerKind.Defense } defense =>
                new RunDefenseCommand(defense),
            AbilityEffect.Power { TargetCount: not null } counted =>
                AbilityCountedAttackSelection.Start(context, counted),
            AbilityEffect.Power power => Power(context, power),
            AbilityEffect.ThwartGroup { Selection: AbilityThwartSelection.All } group =>
                ThwartAll(context, group),
            _ => new Unsupported($"'{context.SourceFace}' has no structural decision for '{effect.OperationName()}'"),
        };
    }

    internal static AbilityStructuralTransition Simultaneous(
        AbilityStructuralContext context, AbilityEffect.Simultaneous simultaneous)
    {
        if (simultaneous.Effects.Length == 0) return new Complete(context.Frames);
        if (simultaneous.Effects.Length == 1)
            return new RunLeaf(simultaneous.Effects[0], context.Frames.Add(
                new SimultaneousFrame(0, [], [])), context.Position, context.HasContinuation);
        return new Ask(simultaneous, context.Frames.Add(new SimultaneousFrame(
            -1, [.. Enumerable.Range(0, simultaneous.Effects.Length)], [])));
    }

    internal static AbilityStructuralTransition AnswerSimultaneous(
        AbilityStructuralContext context, AbilityEffect.Simultaneous simultaneous, Decision answer)
    {
        var legal = Enumerable.Range(0, simultaneous.Effects.Length).ToHashSet();
        if (answer.IsDecline
            || answer.Affordance != context.Expressions.Source.ObjectId
            || answer.Targets.Count != simultaneous.Effects.Length
            || answer.Targets.Distinct().Count() != simultaneous.Effects.Length
            || answer.Targets.Any(index => !legal.Contains(index)))
        {
            return new Rejected($"'{context.SourceFace}' requires one permutation of all "
                + $"{simultaneous.Effects.Length} simultaneous effects");
        }

        var frames = answer.Targets.Select((index, position) => new SimultaneousFrame(index,
            [.. answer.Targets.Skip(position + 1)], [.. answer.Targets.Take(position)])).ToImmutableArray();
        return new RunOrdered(
            [.. answer.Targets.Select(index => simultaneous.Effects[index])], frames);
    }

    internal static Prompt DescribeSimultaneous(
        AbilityStructuralContext context, AbilityEffect.Simultaneous simultaneous) =>
        new(
            Player: context.Expressions.World.FirstPlayer,
            Asking: Question.Order,
            When: TimingPriority.Untimed,
            Trigger: Steps.CardRevealed,
            Label: $"{context.SourceFace}: order simultaneous effects",
            Cancellable: false,
            Affordances:
            [
                new Affordance(
                    context.Expressions.Source.ObjectId, "Order",
                    context.Expressions.Source.ObjectId, context.Expressions.World.FirstPlayer,
                    "simultaneous effects",
                    new TargetRequest(
                        Enumerable.Range(0, simultaneous.Effects.Length).ToList(),
                        simultaneous.Effects.Length, simultaneous.Effects.Length,
                        Rule: "rr:first-player.3")),
            ]);

    internal static AbilityStructuralPrompt DescribeGenericChoice(
        AbilityStructuralContext context, AbilityEffect choice,
        AbilityContinuationFacts continuation) =>
        AbilityChoicePromptDescription.DescribeChoice(context, choice, continuation);

    internal static AbilityStructuralTransition AnswerPaymentChoice(
        AbilityStructuralContext context, AbilityEffect.PayOrEffect payment, Decision answer)
    {
        if (answer.IsDecline || answer.Affordance is < 0 or > 1)
            return new Unsupported($"'{context.SourceFace}' did not offer option {answer.Affordance}");
        if (answer.Affordance == 1)
            return new PayOrCommand(false);

        var sources = CardPayment.Generators(context.Expressions.World,
            context.Expressions.World.Facts, context.Expressions.World.Seats[context.Player],
            context.ResourceAbilities, abilitySource: context.Expressions.Source);
        return Resources.Pays(string.Concat(sources.SelectMany(source => source.Generates)),
            payment.Amount, payment.Resources)
            ? new PayOrCommand(true)
            : new Unsupported($"'{context.SourceFace}' cannot pay the offered resources");
    }

    private static long IndirectAmount(AbilityStructuralContext context, AbilityEffect.IndirectDamage damage) =>
        AbilityEventModifiers.DamageAmount(context.Expressions.World,
            context.Expressions.Source, Amount(damage.Amount, context));

    internal static Prompt DescribeIndirectDamage(
        AbilityStructuralContext context, AbilityEffect.IndirectDamage damage)
    {
        var domain = DamageContext(context);
        var eligible = AbilityDamageAndThreatExecution.Assignable(damage.Among, domain);
        long amount = IndirectAmount(context, damage);
        long share = Math.Min(amount, eligible.Sum(card =>
            AbilityDamageAndThreatExecution.Room(card, domain)));
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed,
            Steps.CardRevealed, $"{context.SourceFace}: assign {share} damage", false,
            [new Affordance(context.Expressions.Source.ObjectId, ChooseVerb,
                context.Expressions.Source.ObjectId, World.Scenario, "indirectDamage",
                new TargetRequest([.. eligible.Select(card => card.ObjectId)], (int)share, (int)share,
                    Rule: "rr:indirect-damage.1", AllowRepeated: true,
                    MaximumOccurrences: eligible.ToDictionary(card => card.ObjectId,
                        card => checked((int)AbilityDamageAndThreatExecution.Room(card, domain)))))])
        {
            DisplayQuestion = $"Assign {share} indirect damage",
            Description = $"Assign {share} damage among the eligible characters, up to each character's remaining health.",
        };
    }

    internal static AbilityStructuralTransition AnswerIndirectDamage(
        AbilityStructuralContext context, AbilityEffect.IndirectDamage damage, Decision answer)
    {
        var domain = DamageContext(context);
        var eligible = AbilityDamageAndThreatExecution.Assignable(damage.Among, domain);
        long expected = Math.Min(IndirectAmount(context, damage), eligible.Sum(card =>
            AbilityDamageAndThreatExecution.Room(card, domain)));
        if (answer.IsDecline || answer.Targets.Count != expected)
            return new Unsupported($"'{context.SourceFace}' requires {expected} indirect damage assignment(s) and {answer.Targets.Count} were chosen");
        var assigned = new Dictionary<int, long>();
        foreach (int id in answer.Targets)
        {
            var card = eligible.FirstOrDefault(candidate => candidate.ObjectId == id);
            if (card is null) return new Unsupported($"card {id} cannot be assigned indirect damage from '{context.SourceFace}'");
            long count = assigned.GetValueOrDefault(id) + 1;
            if (count > AbilityDamageAndThreatExecution.Room(card, domain))
                return new Unsupported($"card {id} has insufficient room for indirect damage from '{context.SourceFace}'");
            assigned[id] = count;
        }
        return new AssignedDamageCommand(assigned.ToImmutableDictionary());
    }

    internal static Prompt DescribeThwartChoice(AbilityStructuralContext context, AbilityEffect.ThwartGroup group)
    {
        if (group.Selection == AbilityThwartSelection.Distributed) return AbilityThreatDistribution.Describe(context, group);
        var schemes = Every(group.Schemes, context);
        bool aerial = Traits.Has(context.Expressions.World, context.Expressions.World.Seats[context.Player].IdentityCard,
            "AERIAL", context.Expressions.World.Facts);
        int count = group.Selection == AbilityThwartSelection.Different && aerial && schemes.Count > 1 ? 2 : 1;
        if (group.Selection == AbilityThwartSelection.LegalPractice)
        {
            var hand = context.Expressions.World.Seats[context.Player].Hand.Cards
                .Where(card => card.ObjectId != context.Expressions.Source.ObjectId).ToList();
            return new Prompt(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
                $"{context.SourceFace}: choose cards and a scheme", false,
                schemes.Where(card => card.Tokens.GetValueOrDefault("k_threat") > 0).Select(scheme =>
                    new Affordance(scheme.ObjectId, ChooseVerb, scheme.ObjectId, World.Scenario,
                        scheme.FaceId, new TargetRequest([.. hand.Select(card => card.ObjectId)], 1, Math.Min(5, hand.Count)))).ToList());
        }
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: choose scheme{(count == 1 ? "" : "s")}", false,
            [new Affordance(context.Expressions.Source.ObjectId, ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, group.OperationName(),
                new TargetRequest([.. schemes.Select(card => card.ObjectId)], count, count))]);
    }

    internal static AbilityStructuralTransition AnswerThwartChoice(
        AbilityStructuralContext context, AbilityEffect.ThwartGroup group, Decision answer)
    {
        if (group.Selection == AbilityThwartSelection.Distributed) return AbilityThreatDistribution.Answer(context, group, answer);
        var schemes = Every(group.Schemes, context);
        if (group.Selection == AbilityThwartSelection.LegalPractice)
            return AnswerLegalPractice(context, schemes, answer);
        bool aerial = Traits.Has(context.Expressions.World, context.Expressions.World.Seats[context.Player].IdentityCard,
            "AERIAL", context.Expressions.World.Facts);
        int count = aerial && schemes.Count > 1 ? 2 : 1;
        var selected = answer.Targets.Select(id => schemes.FirstOrDefault(card => card.ObjectId == id)).ToList();
        if (answer.IsDecline || selected.Count != count || selected.Any(card => card is null)
            || selected.Distinct().Count() != selected.Count)
            return new Unsupported($"'{context.SourceFace}' requires {count} different scheme target(s)");
        var first = selected[0]!;
        var admission = context.Admission().WithSelection(first).WithPowerTargets([first]);
        bool full = AbilityResolutionAdmission.ResolutionOf(group.Thwart.Effect, admission)
            == AbilityAdmission.AdmissionResolution.Full;
        return new ThwartSelectionCommand(first.ObjectId,
            [.. (full ? selected.Select(card => card!.ObjectId) : [first.ObjectId])], [], -1);
    }

    private static AbilityStructuralTransition AnswerLegalPractice(
        AbilityStructuralContext context, IReadOnlyList<Card> schemes, Decision answer)
    {
        var scheme = schemes.FirstOrDefault(card => card.ObjectId == answer.Affordance);
        var hand = context.Expressions.World.Seats[context.Player].Hand;
        if (answer.IsDecline || scheme is null || answer.Targets.Count is < 1 or > 5
            || answer.Targets.Distinct().Count() != answer.Targets.Count
            || answer.Targets.Any(id => id == context.Expressions.Source.ObjectId
                || context.Expressions.World.Cards[id].Area != hand))
            return new Unsupported(
                $"'{context.SourceFace}' requires one to five distinct hand cards");
        return new ThwartSelectionCommand(
            scheme.ObjectId, [scheme.ObjectId], [.. answer.Targets], answer.Targets.Count);
    }

    internal static Prompt DescribeMakeTheCall(AbilityStructuralContext context)
    {
        var world = context.Expressions.World;
        var offers = MakeTheCallOffers.AlliesInPlayerDiscards(world)
            .Select(ally => (Ally: ally, Sources: MakeTheCallOffers.Sources(
                world, context.Player, context.Expressions.Source, ally,
                context.ResourceAbilities)))
            .Where(candidate => MakeTheCallOffers.Affordable(world, candidate.Ally, candidate.Sources))
            .Select(candidate => new Affordance(candidate.Ally.ObjectId, ChooseVerb,
                candidate.Ally.ObjectId, candidate.Ally.Owner, candidate.Ally.FaceId,
                Costs: [MakeTheCallOffers.Price(world, candidate.Ally, candidate.Sources)])).ToList();
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: choose an ally", false, offers);
    }

    internal static AbilityStructuralTransition AnswerMakeTheCall(
        AbilityStructuralContext context, Decision answer)
    {
        var offered = MakeTheCallOffers.AlliesInPlayerDiscards(context.Expressions.World)
            .Select(ally => (Ally: ally, Sources: MakeTheCallOffers.Sources(
                context.Expressions.World, context.Player, context.Expressions.Source, ally,
                context.ResourceAbilities)))
            .Where(candidate => MakeTheCallOffers.Affordable(
                context.Expressions.World, candidate.Ally, candidate.Sources))
            .Select(candidate => candidate.Ally.ObjectId).ToHashSet();
        return !answer.IsDecline && offered.Contains(answer.Affordance)
            ? new MakeTheCallCommand(answer.Affordance)
            : new Unsupported($"'{context.SourceFace}' did not offer ally {answer.Affordance}");
    }


}
