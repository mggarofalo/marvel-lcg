using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

// One immutable read of a live resolution. The runner refreshes it after every
// command, so structural decisions never retain a stale view of the board.
internal sealed record AbilityStructuralContext(
    AbilityProgram Program,
    AbilityExpressionContext Expressions,
    AbilityReachabilityContext Reachability,
    string Trigger,
    string SourceFace,
    string AbilityFace,
    int Player,
    int Position,
    bool HasContinuation,
    AbilityType? Tier,
    string? Power,
    Card? AbilityActor,
    bool HasPendingDependency,
    ImmutableHashSet<AbilityEffect> CrisisIgnoringThwarts,
    ImmutableHashSet<int> PersistedCrisisIgnoringThwarts,
    ImmutableArray<AbilityStructuralFrame> Frames)
{
    internal AbilityAdmissionContext Admission() => new(
        Program, Expressions, Reachability, Power, HasContinuation);
}

internal enum AbilityStructuralOutcome { None, Partial, Full }

// Live frames are typed. Their legacy string encoding is confined to the
// continuation codec at the persistence boundary.
internal abstract record AbilityStructuralFrame;
internal sealed record SequenceFrame(int Next, int Count) : AbilityStructuralFrame;
internal sealed record SimultaneousFrame(int Current, ImmutableArray<int> Remaining, ImmutableArray<int> Completed)
    : AbilityStructuralFrame;
internal sealed record DependentFrame(bool OnFull, bool Predecessor, AbilityStructuralOutcome? Outcome)
    : AbilityStructuralFrame;
internal sealed record ConditionalFrame(bool Then) : AbilityStructuralFrame;
internal sealed record ForEachFrame(long Next, long Count) : AbilityStructuralFrame;
internal sealed record EachTimeFrame(long Next, long Count, int? DiscardedCard) : AbilityStructuralFrame;
internal sealed record ChoiceFrame(int? Option, int? Card) : AbilityStructuralFrame;
internal sealed record ChoiceOtherwiseFrame : AbilityStructuralFrame;
internal sealed record DefenseFrame : AbilityStructuralFrame;
internal sealed record EachPlayerFrame(int Player, bool Final) : AbilityStructuralFrame;

internal sealed record AbilityStructuralObservation(bool Suspended, Card? Discarded = null);

internal abstract record AbilityStructuralTransition;
internal sealed record RunLeaf(
    AbilityEffect Effect, ImmutableArray<AbilityStructuralFrame> Frames,
    int Position, bool HasContinuation,
    AbilityAdmissionResult? Admission = null) : AbilityStructuralTransition;
internal sealed record RunCombinedForEach(AbilityEffect Effect, long Multiplier)
    : AbilityStructuralTransition;
internal sealed record RunChoice(
    AbilityEffect Effect, ChoiceFrame Frame, Card? Selection,
    bool BindsPlayerSelection, AbilityStructuralOutcome? PendingOutcome,
    AbilityAdmissionResult Admission) : AbilityStructuralTransition;
internal sealed record Ask(AbilityEffect Choice, ImmutableArray<AbilityStructuralFrame> Frames)
    : AbilityStructuralTransition;
internal sealed record ScheduleEachPlayer(
    AbilityEffect.EachPlayer Effect, EachPlayerFrame Frame) : AbilityStructuralTransition;
internal sealed record DelayAfterActivation(AbilityEffect.AfterActivation Effect)
    : AbilityStructuralTransition;
internal sealed record RunOrdered(
    ImmutableArray<AbilityEffect> Effects, ImmutableArray<SimultaneousFrame> Frames)
    : AbilityStructuralTransition;
internal sealed record ResolveSpecialsCommand(ImmutableArray<int> Targets) : AbilityStructuralTransition;
internal sealed record ChooseTopForHandCommand(int Selected, ImmutableArray<int> Top)
    : AbilityStructuralTransition;
internal sealed record ShuffleDiscardCommand(ImmutableArray<int> Targets)
    : AbilityStructuralTransition;
internal sealed record PayOrCommand(bool Pay) : AbilityStructuralTransition;
internal sealed record AssignedDamageCommand(ImmutableDictionary<int, long> Assigned)
    : AbilityStructuralTransition;
internal sealed record ThwartSelectionCommand(
    int Scheme, ImmutableArray<int> Resolving, ImmutableArray<int> Discard, long PowerAmount)
    : AbilityStructuralTransition;
internal sealed record MakeTheCallCommand(int Ally) : AbilityStructuralTransition;
internal sealed record StartSequenceCommand(AbilityEffect.Sequence Effect) : AbilityStructuralTransition;
internal sealed record StartDependentCommand(AbilityEffect.Dependent Effect) : AbilityStructuralTransition;
internal sealed record StartForEachCommand(AbilityEffect.ForEach Effect) : AbilityStructuralTransition;
internal sealed record StartEachTimeCommand(AbilityEffect.EachTime Effect) : AbilityStructuralTransition;
internal sealed record RunDefenseCommand(AbilityEffect.Power Effect) : AbilityStructuralTransition;
internal sealed record SchedulePowerCommand(
    AbilityEffect.Power Effect, string Verb, Card Target, ImmutableArray<Card> Targets,
    long Amount, int AbilityIndex, int PowerOrdinal, bool AutomaticThwartTarget)
    : AbilityStructuralTransition;
internal sealed record ActivationTarget(Card Enemy, int Seat);
internal sealed record ScheduleActivationsCommand(
    AbilityEffect.ActivateEnemies Effect, ImmutableArray<ActivationTarget> Targets,
    int Against, bool First, bool Dynamic) : AbilityStructuralTransition;
internal sealed record DiscardEachTime(AbilityEffect.DiscardTop Effect, EachTimeFrame Frame)
    : AbilityStructuralTransition;
internal sealed record Complete(
    ImmutableArray<AbilityStructuralFrame> Frames,
    AbilityAdmissionResult? Admission = null) : AbilityStructuralTransition;
internal sealed record Rejected(string Reason) : AbilityStructuralTransition;
internal sealed record Unsupported(string Reason) : AbilityStructuralTransition;

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
            return Ask(context, effect);
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
            AbilityEffect.PayOrEffect payment => Ask(context, payment),
            AbilityEffect.ChooseTopForHand top => ChooseTopForHand(context, top),
            AbilityEffect.ChooseDiscardToShuffle discard => Ask(context, discard),
            AbilityEffect.ThwartGroup group when group.OperationName() == "thwartDifferentSchemes" =>
                Ask(context, group),
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
        var sources = CardPlay.Generators(world, world.Facts, world.Seats[context.Player]);
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

        var sources = CardPlay.Generators(context.Expressions.World,
            context.Expressions.World.Facts, context.Expressions.World.Seats[context.Player]);
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
        {
            var scheme = schemes.FirstOrDefault(card => card.ObjectId == answer.Affordance);
            var hand = context.Expressions.World.Seats[context.Player].Hand;
            if (answer.IsDecline || scheme is null || answer.Targets.Count is < 1 or > 5
                || answer.Targets.Distinct().Count() != answer.Targets.Count
                || answer.Targets.Any(id => id == context.Expressions.Source.ObjectId || context.Expressions.World.Cards[id].Area != hand))
                return new Unsupported($"'{context.SourceFace}' requires one to five distinct hand cards");
            return new ThwartSelectionCommand(scheme.ObjectId, [scheme.ObjectId], [.. answer.Targets], answer.Targets.Count);
        }
        bool aerial = Traits.Has(context.Expressions.World, context.Expressions.World.Seats[context.Player].IdentityCard,
            "AERIAL", context.Expressions.World.Facts);
        int count = aerial && schemes.Count > 1 ? 2 : 1;
        var selected = answer.Targets.Select(id => schemes.FirstOrDefault(card => card.ObjectId == id)).ToList();
        if (answer.IsDecline || selected.Count != count || selected.Any(card => card is null)
            || selected.Distinct().Count() != selected.Count)
            return new Unsupported($"'{context.SourceFace}' requires {count} different scheme target(s)");
        var first = selected[0]!;
        var admission = context.Admission().WithSelection(first).WithPowerTargets([first]);
        bool full = AbilityInitiation.ResolutionOf(group.Thwart.Effect, admission)
            == AbilityInitiation.ResolutionOutcome.Full;
        return new ThwartSelectionCommand(first.ObjectId,
            [.. (full ? selected.Select(card => card!.ObjectId) : [first.ObjectId])], [], -1);
    }

    internal static Prompt DescribeMakeTheCall(AbilityStructuralContext context)
    {
        var world = context.Expressions.World;
        var offers = AbilityExpressionEvaluation.AlliesInPlayerDiscards(world)
            .Select(ally => (Ally: ally, Sources: AbilityExpressionEvaluation.MakeTheCallSources(
                world, context.Player, context.Expressions.Source, ally)))
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
                context.Expressions.World, context.Player, context.Expressions.Source, ally)))
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
        if (AbilityInitiation.SuspendsPowerEffect(
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
            .SelectMany(candidate => AbilityInitiation.PowerEffects(candidate.Ability.Effect, verb)
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

    private static AbilityStructuralTransition ThwartAll(
        AbilityStructuralContext context, AbilityEffect.ThwartGroup group)
    {
        var schemes = Every(group.Schemes, context);
        return schemes.Count == 0
            ? new Complete(context.Frames)
            : Power(context, group.Thwart, schemes[0], schemes);
    }

    private static AbilityStructuralTransition Activation(
        AbilityStructuralContext context, AbilityEffect.ActivateEnemies activation,
        IReadOnlyList<Card>? ordered = null)
    {
        Card? against = activation.Against is { } named ? Find(named, context) : null;
        int seat = activation.Against switch
        {
            AbilityCardSelection.Bound { Binding: AbilityCardBinding.TriggerActor } =>
                context.Expressions.Occurrence.ActorFacts?.Controller ?? World.Scenario,
            AbilityCardSelection.Bound { Binding: AbilityCardBinding.TriggerTarget } =>
                context.Expressions.Occurrence.TargetFacts?.Controller ?? World.Scenario,
            _ => context.Player,
        };
        if (seat < 0)
            return new Unsupported(
                $"'{context.SourceFace}' initiates an enemy attack against a character with no attacked player");

        var enemies = ordered ?? ActivationCandidates(context, activation);
        if (enemies.Count > 1 && ordered is null)
            return Ask(context, activation);

        var targets = enemies.Select(enemy => new ActivationTarget(
                enemy, activation.EngagedHero ? enemy.Area.PlayArea.Player : seat))
            .Where(target => target.Seat >= 0
                && (!activation.EngagedHero || Forms.In(
                    context.Expressions.World,
                    context.Expressions.World.Seats[target.Seat],
                    context.Expressions.World.Facts, Forms.Hero)))
            .ToImmutableArray();
        return new ScheduleActivationsCommand(
            activation, targets, against?.ObjectId ?? -1,
            activation.First, activation.Dynamic);
    }

    private static List<Card> ActivationCandidates(
        AbilityStructuralContext context, AbilityEffect.ActivateEnemies activation) =>
        Every(activation.Enemies, context).Where(enemy => !activation.Dynamic
            || context.Expressions.Results.GetValueOrDefault($"dynamicActivation:{enemy.ObjectId}") == 0).ToList();

    private static ImmutableArray<CompiledCardAbility> Abilities(
        AbilityStructuralContext context) =>
        string.Equals(context.AbilityFace, context.Expressions.Source.FaceId,
            StringComparison.Ordinal)
            ? [.. AbilityProgramQueries.On(context.Program, context.Expressions.Source)]
            : context.Program.On(context.AbilityFace);

    private static AbilityDamageAndThreatContext DamageContext(AbilityStructuralContext context) =>
        new(context.Expressions, context.Trigger, [], context.AbilityActor, null, context.Power,
            context.HasContinuation, null, null, 0);

    private static Prompt DescribeSpecials(AbilityStructuralContext context, AbilityEffect.CardAction specials)
    {
        var cards = Every(specials.Selection, context);
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed,
            Steps.ResolveSpecial, $"{context.SourceFace}: order Special abilities", false,
            [new Affordance(context.Expressions.Source.ObjectId, ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, specials.OperationName(),
                new TargetRequest([.. cards.Select(card => card.ObjectId)], cards.Count, cards.Count))]);
    }

    private static AbilityStructuralTransition AnswerSpecials(
        AbilityStructuralContext context, AbilityEffect.CardAction specials, Decision answer)
    {
        var legal = Every(specials.Selection, context).Select(card => card.ObjectId).ToHashSet();
        return answer.IsDecline || answer.Targets.Count != legal.Count
            || answer.Targets.Distinct().Count() != legal.Count
            || answer.Targets.Any(id => !legal.Contains(id))
            ? new Unsupported($"'{context.SourceFace}' requires one permutation of all {legal.Count} Special abilities")
            : new ResolveSpecialsCommand([.. answer.Targets]);
    }

    private static Prompt DescribeTopForHand(AbilityStructuralContext context, AbilityEffect.ChooseTopForHand top)
    {
        var cards = TopCards(context.Expressions.World.Seats[context.Player].Deck, top.Count);
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: choose a top card", false,
            cards.Select(card => new Affordance(card.ObjectId, ChooseVerb,
                card.ObjectId, context.Player, card.FaceId)).ToList())
        { ExposesConcealedCandidates = true };
    }

    private static AbilityStructuralTransition AnswerTopForHand(
        AbilityStructuralContext context, AbilityEffect.ChooseTopForHand top, Decision answer)
    {
        var cards = TopCards(context.Expressions.World.Seats[context.Player].Deck, top.Count);
        return answer.IsDecline || cards.All(card => card.ObjectId != answer.Affordance)
            ? new Unsupported($"'{context.SourceFace}' did not offer card {answer.Affordance} among its top cards")
            : new ChooseTopForHandCommand(answer.Affordance, [.. cards.Select(card => card.ObjectId)]);
    }

    private static Prompt DescribeDiscardShuffle(
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

    private static AbilityStructuralTransition AnswerDiscardShuffle(
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

    internal static AbilityStructuralTransition AnswerGenericChoice(
        AbilityStructuralContext context, AbilityEffect choice,
        AbilityContinuationFacts continuation, Decision answer) =>
        AbilityStructuralQueries.AnswerChoice(context, choice, continuation, answer);

    internal static AbilityStructuralTransition Choose(
        AbilityStructuralContext context, AbilityEffect.Choose choice)
    {
        if (choice.Options.Length < 2)
            return new Rejected($"'{context.SourceFace}' offers a choice of one, which is not a choice");
        if (choice.Options.Any(option => AbilityInitiation.IsOptionLegal(option, context.Admission())))
            return Ask(context, choice);

        bool mandatoryEncounter = !AbilityCardQueries.IsPlayerCard(
            context.Expressions.World.Facts, context.Expressions.Source)
            && context.Tier is { } tier && AbilityTypes.IsMandatory(tier);
        return mandatoryEncounter ? new Complete(context.Frames) : new Rejected(
            $"'{context.SourceFace}' requires a choice and has no legal option");
    }

    internal static AbilityStructuralTransition ChooseCard(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice) =>
        AbilityInitiation.LegalCardChoices(choice, context.Admission()).Count == 0
            ? new Complete(context.Frames)
            : Ask(context, choice);

    internal static AbilityStructuralTransition EachPlayer(
        AbilityStructuralContext context, AbilityEffect.EachPlayer each)
    {
        if (AbilityInitiation.HasNestedEachPlayer(each, context.Admission()))
            return new Unsupported($"'{context.SourceFace}' nests one each-player frame inside another, which is not implemented");
        return new ScheduleEachPlayer(each, new EachPlayerFrame(context.Player, false));
    }

    private static AbilityStructuralTransition ResolveSpecials(
        AbilityStructuralContext context, AbilityEffect.CardAction specials) =>
        Every(specials.Selection, context).Count == 0 ? new Complete(context.Frames) : Ask(context, specials);

    private static AbilityStructuralTransition ChooseTopForHand(
        AbilityStructuralContext context, AbilityEffect.ChooseTopForHand top) =>
        TopCards(context.Expressions.World.Seats[context.Player].Deck, top.Count).Count == 0
            ? new Complete(context.Frames) : Ask(context, top);

    private static AbilityStructuralTransition AfterActivation(
        AbilityStructuralContext context, AbilityEffect.AfterActivation after) =>
        context.Expressions.World.Activation is null
            ? new Unsupported($"'{context.SourceFace}' delays an effect and no enemy is activating")
            : new DelayAfterActivation(after);

    private static Ask Ask(AbilityStructuralContext context, AbilityEffect choice) =>
        new(choice, context.Frames.Add(new ChoiceFrame(null, null)));

    internal static AbilityStructuralTransition SequenceStart(
        AbilityStructuralContext context, AbilityEffect.Sequence sequence, int from)
    {
        var admission = from == 0
            ? AbilityInitiation.AdmitStructure(sequence, context.Admission())
            : null;
        return NextSequence(context, sequence,
            new SequenceFrame(from, sequence.Effects.Length),
            new AbilityStructuralObservation(false), admission);
    }

    internal static AbilityStructuralTransition NextSequence(
        AbilityStructuralContext context, AbilityEffect.Sequence sequence,
        SequenceFrame frame, AbilityStructuralObservation observation,
        AbilityAdmissionResult? admission = null)
    {
        if (observation.Suspended)
            return new Complete(context.Frames.Add(frame), admission);
        if (frame.Next < 0 || frame.Count != sequence.Effects.Length || frame.Next > frame.Count)
            return new Rejected("invalid sequence cursor");
        if (frame.Next == frame.Count)
            return new Complete(context.Frames.Add(frame), admission);

        int position = frame.Next;
        var next = new SequenceFrame(position + 1, frame.Count);
        return new RunLeaf(
            sequence.Effects[position], context.Frames.Add(next), position,
            context.HasContinuation || position < frame.Count - 1, admission);
    }

    internal static AbilityStructuralTransition NextSimultaneous(
        AbilityStructuralContext context, AbilityEffect.Simultaneous simultaneous,
        SimultaneousFrame frame, AbilityStructuralObservation observation)
    {
        if (observation.Suspended)
            return new Complete(context.Frames.Add(frame));
        if (frame.Current < 0 || frame.Current >= simultaneous.Effects.Length
            || frame.Remaining.Any(index => index < 0 || index >= simultaneous.Effects.Length)
            || frame.Completed.Any(index => index < 0 || index >= simultaneous.Effects.Length)
            || frame.Completed.Append(frame.Current).Concat(frame.Remaining).Distinct().Count()
                != simultaneous.Effects.Length)
            return new Rejected("invalid simultaneous cursor");
        if (frame.Remaining.IsEmpty)
            return new Complete(context.Frames.Add(frame));

        int current = frame.Remaining[0];
        var next = new SimultaneousFrame(
            current, frame.Remaining.RemoveAt(0), frame.Completed.Add(frame.Current));
        return new RunLeaf(
            simultaneous.Effects[current], context.Frames.Add(next), context.Position,
            context.HasContinuation || next.Remaining.Length > 0);
    }

    internal static AbilityStructuralTransition Conditional(
        AbilityStructuralContext context, AbilityEffect.Conditional conditional)
    {
        bool then = Test(conditional.Test, context);
        AbilityEffect? selected = then ? conditional.Then : conditional.Else;
        var frames = context.Frames.Add(new ConditionalFrame(then));
        return selected is null
            ? new Complete(frames)
            : new RunLeaf(selected, frames, context.Position, context.HasContinuation);
    }

    internal static AbilityStructuralTransition Dependent(
        AbilityStructuralContext context, AbilityEffect.Dependent dependent)
    {
        var admission = context.Admission();
        var effect = dependent.Effect;
        if (AbilityInitiation.ActiveChoices(effect, admission).Any())
        {
            AbilityInitiation.PreflightAnsweredOutcome(effect, admission);
            AbilityInitiation.PreflightContinuationBoundaries(dependent.Continuation, admission);
            return DependentLeaf(context, dependent, effect,
                predecessor: true, outcome: null,
                hasContinuation: context.HasContinuation);
        }

        var required = dependent.OnFull
            ? AbilityInitiation.ResolutionOutcome.Full
            : AbilityInitiation.ResolutionOutcome.None;
        var outcome = AbilityInitiation.EnsureDependentSupported(
            dependent, admission, effect, dependent.Continuation, required);
        var structural = (AbilityStructuralOutcome)(int)outcome;
        if (outcome == AbilityInitiation.ResolutionOutcome.None)
        {
            return outcome == required
                ? DependentLeaf(context, dependent, dependent.Continuation,
                    predecessor: false, structural, context.HasContinuation)
                : new Complete(context.Frames);
        }

        return DependentLeaf(context, dependent, effect,
            predecessor: true, structural,
            hasContinuation: context.HasContinuation);
    }

    internal static AbilityStructuralTransition AfterDependentLeaf(
        AbilityStructuralContext context, AbilityEffect.Dependent dependent,
        DependentFrame frame, AbilityStructuralObservation observation)
    {
        if (observation.Suspended || !frame.Predecessor)
            return new Complete(context.Frames);
        var required = dependent.OnFull
            ? AbilityStructuralOutcome.Full
            : AbilityStructuralOutcome.None;
        return frame.Outcome == required
            ? DependentLeaf(context, dependent, dependent.Continuation,
                predecessor: false, frame.Outcome, context.HasContinuation)
            : new Complete(context.Frames);
    }

    internal static AbilityStructuralTransition ForEachStart(
        AbilityStructuralContext context, AbilityEffect.ForEach repeated)
    {
        long count = AbilityInitiation.NonNegativeForEachCount(
            Amount(repeated.Count, context));
        if (count == 0)
            return new Complete(context.Frames);

        if (!AbilityInitiation.Choices(repeated.Effect).Any())
        {
            if (repeated.Effect is AbilityEffect.Damage or AbilityEffect.RemoveThreat)
                return new RunCombinedForEach(repeated.Effect, count);
            if (AbilityInitiation.ContainsForEachTarget(repeated.Effect))
                return new Unsupported(
                    $"'{context.SourceFace}' has a targeted for-each effect without choose "
                    + "whose one target cannot be persisted");
        }

        return NextForEach(context, repeated, new ForEachFrame(0, count),
            new AbilityStructuralObservation(false));
    }

    internal static AbilityStructuralTransition NextForEach(
        AbilityStructuralContext context, AbilityEffect.ForEach repeated,
        ForEachFrame frame, AbilityStructuralObservation observation)
    {
        if (observation.Suspended || frame.Next >= frame.Count)
            return new Complete(context.Frames.Add(frame));

        long iteration = frame.Next;
        var next = new ForEachFrame(iteration + 1, frame.Count);
        return new RunLeaf(
            repeated.Effect, context.Frames.Add(next), context.Position,
            context.HasContinuation || iteration < frame.Count - 1);
    }

    internal static AbilityStructuralTransition EachTimeStart(
        AbilityStructuralContext context, AbilityEffect.EachTime repeated, long from,
        long? persistedCount = null)
    {
        if (repeated.Effect is not AbilityEffect.DiscardTop
            { From: AbilitySearchArea.EncounterDeck, Players: null } discard)
            return new Unsupported(
                $"'{context.SourceFace}' uses each-time around an unsupported preceding effect");

        long requested = persistedCount ?? Amount(discard.Count, context);
        if (requested < 0)
            return new Rejected("'eachTime' needs a non-negative discard count");
        if (requested == 0)
            return new Complete(context.Frames);
        if (persistedCount is null)
            AbilityInitiation.ValidateEachTimeBody(repeated, context.Admission());

        long count = requested;
        if (persistedCount is null)
        {
            var deck = context.Expressions.World.AreaOf(DeckType.EncounterDeck);
            var pile = context.Expressions.World.AreaOf(DeckType.EncounterDiscardPile);
            long available = deck.Cards.Count > 0 ? deck.Cards.Count : pile.Cards.Count;
            count = Math.Min(requested, available);
        }
        return NextEachTime(context, repeated, new EachTimeFrame(from, count, null),
            new AbilityStructuralObservation(false));
    }

    internal static AbilityStructuralTransition NextEachTime(
        AbilityStructuralContext context, AbilityEffect.EachTime repeated,
        EachTimeFrame frame, AbilityStructuralObservation observation)
    {
        if (observation.Suspended || frame.Next >= frame.Count)
            return new Complete(context.Frames.Add(frame));

        return new DiscardEachTime(
            new AbilityEffect.DiscardTop(
                AbilitySearchArea.EncounterDeck, Players: null,
                new AbilityNumber.Constant(1)), frame);
    }

    internal static AbilityStructuralTransition AfterEachTimeDiscard(
        AbilityStructuralContext context, AbilityEffect.EachTime repeated,
        EachTimeFrame frame, AbilityStructuralObservation observation)
    {
        if (observation.Discarded is not { } discarded)
            return new Complete(context.Frames.Add(frame));

        var next = new EachTimeFrame(frame.Next + 1, frame.Count, discarded.ObjectId);
        if (!Test(repeated.When, context))
            return NextEachTime(context, repeated, next,
                new AbilityStructuralObservation(false));

        return new RunLeaf(
            repeated.Then, context.Frames.Add(next), context.Position,
            context.HasContinuation || frame.Next < frame.Count - 1);
    }

    private static RunLeaf DependentLeaf(
        AbilityStructuralContext context, AbilityEffect.Dependent dependent,
        AbilityEffect effect, bool predecessor, AbilityStructuralOutcome? outcome,
        bool hasContinuation)
    {
        var frame = new DependentFrame(dependent.OnFull, predecessor, outcome);
        return new RunLeaf(effect, context.Frames.Add(frame),
            context.Position, hasContinuation);
    }

    private static long Amount(AbilityNumber number, AbilityStructuralContext context)
    {
        var evaluation = Evaluation(context);
        return Publish(evaluation.Result(evaluation.Amount(number)), context.Expressions.World);
    }

    private static bool Test(AbilityCondition condition, AbilityStructuralContext context)
    {
        var evaluation = Evaluation(context);
        return Publish(evaluation.Result(evaluation.Test(condition)), context.Expressions.World);
    }

    private static AbilityExpressionEvaluation Evaluation(AbilityStructuralContext context) =>
        new(context.Expressions, new AbilitySelectorEvaluation(context.Expressions.Bindings));

    private static IReadOnlyList<Card> Every(AbilityCardSelection selection, AbilityStructuralContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(
            context.Expressions.Bindings, program: context.Program);
        return Publish(evaluation.Result(evaluation.Every(selection)), context.Expressions.World);
    }

    private static Card? Find(
        AbilityCardSelection selection, AbilityStructuralContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(
            context.Expressions.Bindings, program: context.Program);
        return Publish(evaluation.Result(evaluation.Find(selection)), context.Expressions.World);
    }

    private static List<Card> TopCards(Area deck, long count) =>
        [.. deck.Cards.TakeLast(checked((int)Math.Max(0, count))).Reverse()];

    private static T Publish<T>(AbilityQueryResult<T> result, World world)
    {
        foreach (var observation in result.Information)
            world.RecordInformation(observation);
        return result.Value;
    }
}
