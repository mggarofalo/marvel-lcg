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
            or "chooseTopForHand" or "chooseDiscardToShuffle"
            or "thwartDifferentSchemes" or "makeTheCall" or "legalPractice"
            or "attack" or "defense" or "thwart" or "thwartSchemes"
            or "placeThreat" or "enemyAttacks" or "enemySchemes");

    internal static AbilityStructuralTransition Decide(AbilityStructuralContext context, AbilityEffect effect)
    {
        if (effect.OperationName() is "makeTheCall" or "legalPractice")
            return AskFor(context, effect);
        return effect switch
        {
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
                ResolveSpecials(context, specials),
            AbilityEffect.PayOrEffect payment => AskFor(context, payment),
            AbilityEffect.ChooseTopForHand top => ChooseTopForHand(context, top),
            AbilityEffect.ChooseDiscardToShuffle discard => AskFor(context, discard),
            AbilityEffect.ThwartGroup group when group.OperationName() == "thwartDifferentSchemes" =>
                AskFor(context, group),
            AbilityEffect.AfterActivation after => AfterActivation(context, after),
            AbilityEffect.ActivateEnemies activation => Activation(context, activation),
            AbilityEffect.Power { Kind: AbilityPowerKind.Defense } defense =>
                new RunDefenseCommand(defense),
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
        AbilityStructuralQueries.DescribeChoice(context, choice, continuation);

    internal static Prompt DescribeSpecialChoice(
        AbilityStructuralContext context, AbilityEffect choice) => choice switch
        {
            AbilityEffect.CardAction { Instruction: AbilityCardInstruction.ResolveSpecials } specials =>
                DescribeSpecials(context, specials),
            AbilityEffect.ChooseTopForHand top => DescribeTopForHand(context, top),
            AbilityEffect.ChooseDiscardToShuffle discard => DescribeDiscardShuffle(context, discard),
            _ => throw new InvalidOperationException($"'{context.SourceFace}' has no special prompt for '{choice.OperationName()}'"),
        };

    internal static AbilityStructuralTransition AnswerSpecialChoice(
        AbilityStructuralContext context, AbilityEffect choice, Decision answer) => choice switch
        {
            AbilityEffect.CardAction { Instruction: AbilityCardInstruction.ResolveSpecials } specials =>
                AnswerSpecials(context, specials, answer),
            AbilityEffect.ChooseTopForHand top => AnswerTopForHand(context, top, answer),
            AbilityEffect.ChooseDiscardToShuffle discard => AnswerDiscardShuffle(context, discard, answer),
            _ => new Unsupported($"'{context.SourceFace}' has no special answer for '{choice.OperationName()}'"),
        };

    internal static Prompt DescribePaymentChoice(
        AbilityStructuralContext context, AbilityEffect.PayOrEffect payment)
    {
        var world = context.Expressions.World;
        var sources = CardPayment.Generators(
            world, world.Facts, world.Seats[context.Player], context.ResourceAbilities);
        bool payable = Resources.Pays(string.Concat(sources.SelectMany(source => source.Generates)),
            payment.Resources.Length, payment.Resources);
        var offers = new List<Affordance>();
        if (payable)
        {
            offers.Add(new Affordance(0, ChooseVerb,
                context.Expressions.Source.ObjectId, World.Scenario, "spend",
                Costs: [new CostOption(context.Expressions.Source.ObjectId,
                    payment.Resources.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    [payment.Resources], Sources: sources)]));
        }
        if (!payment.ExhaustOnly)
        {
            offers.Add(new Affordance(1, ChooseVerb,
                context.Expressions.Source.ObjectId, World.Scenario, "effect"));
        }
        else if (payment.Otherwise is AbilityEffect.CardAction exhaust
            && Every(exhaust.Selection, context).Any(card => card.Ready))
        {
            offers.Add(new Affordance(1, ChooseVerb,
                context.Expressions.Source.ObjectId, World.Scenario, "exhaust"));
        }
        return new Prompt(context.Player, Question.Option, TimingPriority.Untimed,
            Steps.CardRevealed, $"{context.SourceFace}: spend or "
            + (payment.ExhaustOnly ? "exhaust" : "resolve"), false, offers);
    }

    internal static AbilityStructuralTransition AnswerPaymentChoice(
        AbilityStructuralContext context, AbilityEffect.PayOrEffect payment, Decision answer)
    {
        if (answer.IsDecline || answer.Affordance is < 0 or > 1)
            return new Unsupported($"'{context.SourceFace}' did not offer option {answer.Affordance}");
        if (answer.Affordance == 1)
            return new PayOrCommand(false);

        var sources = CardPayment.Generators(context.Expressions.World,
            context.Expressions.World.Facts, context.Expressions.World.Seats[context.Player],
            context.ResourceAbilities);
        return Resources.Pays(string.Concat(sources.SelectMany(source => source.Generates)),
            payment.Resources.Length, payment.Resources)
            ? new PayOrCommand(true)
            : new Unsupported($"'{context.SourceFace}' cannot pay the offered resources");
    }

    internal static Prompt DescribeIndirectDamage(
        AbilityStructuralContext context, AbilityEffect.IndirectDamage damage)
    {
        var domain = DamageContext(context);
        var eligible = AbilityDamageAndThreatExecution.Assignable(damage.Among, domain);
        long amount = Amount(damage.Amount, context);
        long share = Math.Min(amount, eligible.Sum(card =>
            AbilityDamageAndThreatExecution.Room(card, domain)));
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed,
            Steps.CardRevealed, $"{context.SourceFace}: assign {share} damage", false,
            [new Affordance(context.Expressions.Source.ObjectId, ChooseVerb,
                context.Expressions.Source.ObjectId, World.Scenario, "indirectDamage",
                new TargetRequest([.. eligible.Select(card => card.ObjectId)], (int)share, (int)share,
                    Rule: "rr:indirect-damage.1", AllowRepeated: true,
                    MaximumOccurrences: eligible.ToDictionary(card => card.ObjectId,
                        card => checked((int)AbilityDamageAndThreatExecution.Room(card, domain)))))]);
    }

    internal static AbilityStructuralTransition AnswerIndirectDamage(
        AbilityStructuralContext context, AbilityEffect.IndirectDamage damage, Decision answer)
    {
        var domain = DamageContext(context);
        var eligible = AbilityDamageAndThreatExecution.Assignable(damage.Among, domain);
        long expected = Math.Min(Amount(damage.Amount, context), eligible.Sum(card =>
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
        var offers = AbilityExpressionEvaluation.AlliesInPlayerDiscards(world)
            .Select(ally => (Ally: ally, Sources: AbilityExpressionEvaluation.MakeTheCallSources(
                world, context.Player, context.Expressions.Source, ally,
                context.ResourceAbilities)))
            .Where(candidate => Resources.Pays(string.Concat(candidate.Sources.SelectMany(source => source.Generates)),
                Resources.Cost(candidate.Ally.FaceId, world.Facts, world.Players) ?? 0,
                Resources.Required(world, candidate.Ally, world.Facts)))
            .Select(candidate => new Affordance(candidate.Ally.ObjectId, ChooseVerb,
                candidate.Ally.ObjectId, candidate.Ally.Owner, candidate.Ally.FaceId,
                Costs: [new CostOption(candidate.Ally.ObjectId,
                    (Resources.Cost(candidate.Ally.FaceId, world.Facts, world.Players) ?? 0).ToString(),
                    Resources.Required(world, candidate.Ally, world.Facts) is { Length: > 0 } rule ? [rule] : null,
                    Sources: candidate.Sources)])).ToList();
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: choose an ally", false, offers);
    }

    internal static AbilityStructuralTransition AnswerMakeTheCall(
        AbilityStructuralContext context, Decision answer)
    {
        var offered = AbilityExpressionEvaluation.AlliesInPlayerDiscards(context.Expressions.World)
            .Select(ally => (Ally: ally, Sources: AbilityExpressionEvaluation.MakeTheCallSources(
                context.Expressions.World, context.Player, context.Expressions.Source, ally,
                context.ResourceAbilities)))
            .Where(candidate => Resources.Pays(string.Concat(candidate.Sources.SelectMany(source => source.Generates)),
                Resources.Cost(candidate.Ally.FaceId, context.Expressions.World.Facts, context.Expressions.World.Players) ?? 0,
                Resources.Required(context.Expressions.World, candidate.Ally, context.Expressions.World.Facts)))
            .Select(candidate => candidate.Ally.ObjectId).ToHashSet();
        return !answer.IsDecline && offered.Contains(answer.Affordance)
            ? new MakeTheCallCommand(answer.Affordance)
            : new Unsupported($"'{context.SourceFace}' did not offer ally {answer.Affordance}");
    }

    internal static Prompt DescribeActivationOrder(
        AbilityStructuralContext context, AbilityEffect.ActivateEnemies activation)
    {
        var enemies = ActivationCandidates(context, activation);
        var ids = enemies.Select(card => card.ObjectId).ToList();
        return new Prompt(context.Expressions.World.FirstPlayer, Question.Order, TimingPriority.Untimed,
            Steps.CardRevealed, $"{context.SourceFace}: order enemy activations", false,
            [new Affordance(context.Expressions.Source.ObjectId, "Order", context.Expressions.Source.ObjectId,
                context.Expressions.World.FirstPlayer, "enemy activations",
                new TargetRequest(ids, ids.Count, ids.Count, Rule: "rr:activation.5"))]);
    }

    internal static AbilityStructuralTransition AnswerActivationOrder(
        AbilityStructuralContext context, AbilityEffect.ActivateEnemies activation, Decision answer)
    {
        var legal = ActivationCandidates(context, activation).Select(card => card.ObjectId).ToHashSet();
        if (answer.IsDecline || answer.Affordance != context.Expressions.Source.ObjectId
            || answer.Targets.Count != legal.Count || answer.Targets.Distinct().Count() != legal.Count
            || answer.Targets.Any(id => !legal.Contains(id)))
        {
            return new Unsupported(
                $"'{context.SourceFace}' requires one permutation of all {legal.Count} enemy activations");
        }
        return Activation(context, activation,
            [.. answer.Targets.Select(id => context.Expressions.World.Cards[id])]);
    }

}
