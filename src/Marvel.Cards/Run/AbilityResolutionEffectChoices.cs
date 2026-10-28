using System.Collections.Immutable;
using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionEffectChoices
{
    /// <summary>Suspend an ability for one persisted player choice.</summary>
    internal static void SuspendForChoice(this AbilityResolutionExecution execution, AbilityEffect node, AbilityResolutionState cast)
    {
        int abilityOrdinal = execution.AbilityOrdinal(node, cast);
        var continuation = AbilityContinuations.Step(
            execution.Capture(cast, abilityOrdinal), AbilityContinuationStage.Choose,
            cast.World.Agenda.Current?.Round ?? 0);
        if (cast.World.Agenda.Current is { } owner
            && cast.World.Agenda.Occurrence is { } containing)
        {
            // A triggered ability must finish before the owning window offers another ability.
            cast.World.Agenda.ContinueBeforeOwner(containing, owner, continuation, cast.Occurrence);
        }
        else if (cast.Occurrence.Is(Steps.TurnAction))
        {
            cast.World.Agenda.ThenContinuation(continuation, cast.Occurrence);
        }
        else
        {
            cast.World.Agenda.Then(continuation);
        }

        cast.Suspend();
    }

    internal static IReadOnlyList<Card> DamageTargets(this AbilityResolutionExecution execution, AbilityCardSelection targets, AbilityResolutionState cast) =>
        AbilityDamageAndThreatExecution.DamageTargets(targets, execution.DamageAndThreatContext(cast));

    /// <summary>Deals an assignment that is already worked out.</summary>
    /// <remarks>
    /// In object-id order, because <c>rr:indirect-damage.3</c> resolves it
    /// "simultaneously" and simultaneous still has to reach the event stream in
    /// some order — one the board cannot see and the wire can.
    /// </remarks>
    internal static void Resolve(this AbilityResolutionExecution execution,
        AbilityEffect node, AbilityResolutionState cast, Dictionary<int, long> assigned)
        => execution.ApplyDamageAndThreat(
            AbilityDamageAndThreatExecution.ResolveAssigned(
                node, assigned, execution.DamageAndThreatContext(cast)),
            node, cast);

    /// <summary>"Deal N damage to …" — <c>rr:damage</c>.</summary>
    /// <remarks>
    /// Through <see cref="DamagePlacement.Resolve"/> and not at the token, because damage
    /// is one rule however it arrived: <c>rr:tough.2</c> prevents all of it and
    /// discards a status card instead, and <c>rr:defeat</c> is the other half
    /// of the same moment. A card that wrote to <c>k_damage</c> would skip
    /// both and leave a defeated character standing.
    /// </remarks>
    internal static void DealDamage(this AbilityResolutionExecution execution, AbilityEffect.Damage damage, AbilityEffect node, AbilityResolutionState cast, long multiplier = 1)
        => execution.ApplyDamageAndThreat(
            AbilityDamageAndThreatExecution.DealDamage(
                damage, node, execution.DamageAndThreatContext(cast), multiplier),
            node, cast);

    internal static void SchedulePower(this AbilityResolutionExecution execution, SchedulePowerCommand command, AbilityResolutionState cast)
    {
        var continuationChosen = cast.CaptureCurrentSelection();
        if (command.Target is { } target) cast.Choose(target);
        var capture = execution.Capture(cast, command.AbilityIndex, continuationChosen);
        var power = new CardPowerInvocation(
            execution.Resolver(cast), cast.Source, command.Target, command.Amount, cast.Trigger)
        {
            Performer = cast.AbilityActor,
            Targets = command.Effect.TargetCount is null ? default : command.Targets,
            UsesThwartStat = command.Verb == BasicPowers.ThwartVerb
                && AbilityThwartStatUsage.ReadsActor(command.Effect.Effect, cast.QueryContext(),
                    cast.AbilityActor ?? cast.World.Seats[execution.Resolver(cast)].IdentityCard),
            Continuation = AbilityContinuationScheduling.Power(capture, command, cast.World.Facts),
            SurgeGained = capture.Memory.SurgeGained,
            Origin = capture.Invocation.Occurrence,
        };
        bool scheduled = command.Verb == BasicPowers.AttackVerb
            ? BasicPowerInitiation.CardAttack(
                cast.World, cast.World.Facts, power, cast.Events, nested: true)
            : BasicThwartPowers.CardThwart(
                cast.World, cast.World.Facts, power, cast.Events, cast.Occurrence.Threat,
                command.AutomaticThwartTarget, nested: true);
        if (!scheduled)
        {
            return;
        }

        cast.Suspend();
    }

    internal static void RemoveThreat(this AbilityResolutionExecution execution, AbilityEffect.RemoveThreat removal, AbilityResolutionState cast, long multiplier = 1)
        => execution.ApplyDamageAndThreat(
            AbilityDamageAndThreatExecution.RemoveThreat(
                removal, execution.DamageAndThreatContext(cast), multiplier),
            removal, cast);

    internal static long EventModifier(this AbilityResolutionExecution execution, AbilityResolutionState cast, string kind) =>
        AbilityEventModifiers.Amount(cast.World, cast.Source, kind);

    internal static IReadOnlyList<ContinuousEffect> EventModifierEffects(this AbilityResolutionExecution execution,
        AbilityResolutionState cast, string kind)
        => AbilityEventModifiers.Effects(cast.World, cast.Source, kind);

    /// <summary>Which card type a word names.</summary>
    internal static CardKind Kind(this AbilityResolutionExecution execution, string named) => named switch
    {
        "sideScheme" => CardKind.EncounterSideScheme,
        "minion" => CardKind.Minion,
        "ally" => CardKind.Ally,
        "upgrade" => CardKind.Upgrade,
        "treachery" => CardKind.Treachery,
        _ => throw new RulesNotImplementedException(
            $"'{named}' is not a card type this engine can name"),
    };


    /// <summary>
    /// "The villain attacks you", "the villain schemes" — an enemy activation
    /// a card asked for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:activation.7</c>: an effect initiating an activation resolves after
    /// that activation fully resolves. Each scheduled activation therefore has
    /// a completion sentinel, and the containing ability waits for every named
    /// sentinel before continuing. Defender and interrupt choices may suspend
    /// this work without completing the initiating effect.
    /// </para>
    /// <para>
    /// <b>Which activation is the card's to say.</b> <c>rr:activation.1</c>
    /// reads it off the player's form — attack in hero form, scheme in
    /// alter-ego form — but that rule is about the activation the villain phase
    /// schedules. A card that says "the villain attacks you" has already
    /// chosen, and reading the form here would make Assault do nothing to a
    /// hero who had flipped since the card was dealt.
    /// </para>
    /// <para>
    /// An activation collection suspends for an ordered target request whenever
    /// a read finds several eligible enemies. A dynamic collection re-reads
    /// after that ordered batch, excluding enemies it has already processed.
    /// </para>
    /// </remarks>
    internal static void ScheduleActivations(this AbilityResolutionExecution execution,
        ScheduleActivationsCommand command, AbilityResolutionState cast)
    {
        int round = cast.World.Agenda.Current?.Round ?? 0;
        bool duringActivation = cast.World.Agenda.EnclosingActivationId() >= 0;
        AbilityActivationReceipts.Begin(cast.Results, command.Dynamic);
        var activationIds = EnqueueActivations(execution, command, cast, round);

        cast.World.Effects.BindScheduledActivations(
            cast.Occurrence.Id, cast.Source.ObjectId, activationIds);
        if (activationIds.Count > 0)
        {
            if (duringActivation && !command.First && cast.Execution is { } invocation)
                cast.World.Agenda.Executions.DeferActivation(invocation);
            int abilityOrdinal = execution.AbilityOrdinal(command.Effect, cast);
            var capture = AbilityContinuationScheduling.ForActivations(
                execution.Capture(cast, abilityOrdinal), command.Dynamic, command.Targets.Length);
            cast.World.Agenda.AfterActivations(activationIds, AbilityContinuations.Step(
                capture, AbilityContinuationStage.Resume, round, plan: true, activationIds: activationIds));
            cast.WaitFor(activationIds);
            cast.Suspend();
        }
        else if (command.Dynamic)
        {
            AbilityContinuations.CompleteDynamicActivation(cast.Results);
        }
        else cast.CompletedOutcome = AbilityStructuralOutcome.None;
    }

    private static List<int> EnqueueActivations(AbilityResolutionExecution execution, ScheduleActivationsCommand command,
        AbilityResolutionState cast, int round)
    {
        var activationIds = new List<int>();
        foreach (var target in command.Targets)
        {
            var activation = new PhaseStep(
                command.Effect.Attack ? new EnemyAttackOperation(command.Against)
                    { DealBoostCards = command.Effect.DealBoostCards } : new EnemySchemeOperation(),
                round, 2, Index: target.Seat, Subject: target.Enemy.ObjectId, Seat: target.Seat);
            if (command.Dynamic) cast.Results[$"dynamicActivation:{activation.Subject}"] = 1;
            int activationId = command.First
                ? cast.World.Agenda.NowActivation(activation)
                : cast.World.Agenda.ThenActivation(activation);
            activationIds.Add(activationId);
            if (command.Effect.Modifier is { } modifier)
                cast.World.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                    Kind: command.Effect.Attack ? "attack" : "scheme",
                    Amount: execution.Amount(modifier, cast), Card: cast.Source.ObjectId,
                    Affects: target.Enemy.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfActivation))
                { ActivationId = activationId });
        }
        return activationIds;
    }

    /// <summary>Resume the containing ability after a rules procedure finishes.</summary>
    internal static void SuspendAfterProcedure(this AbilityResolutionExecution execution,
        AbilityEffect node, AbilityResolutionState cast, PhaseStep? agendaOwner = null,
        Occurrence? agendaOccurrence = null)
    {
        int abilityOrdinal = execution.AbilityOrdinal(node, cast);
        var capture = AbilityContinuationScheduling.ForEffectProcedure(
            execution.Capture(cast, abilityOrdinal));
        var continuation = AbilityContinuations.Step(
            capture, AbilityContinuationStage.Resume, cast.World.Agenda.Current?.Round ?? 0, plan: true);
        if (agendaOwner is null)
        {
            cast.World.Agenda.Then(continuation);
        }
        else
        {
            cast.World.Agenda.ContinueBeforeOwner(
                agendaOccurrence
                    ?? throw new InvalidOperationException(
                        "a suspended rules procedure has no containing occurrence"),
                agendaOwner.Value,
                continuation);
        }
        cast.Suspend();
    }

    /// <summary>Resume an initiated ability after its cost procedure settles.</summary>
    internal static void SuspendAfterCost(this AbilityResolutionExecution execution,
        AbilityResolutionState cast, int abilityOrdinal, PhaseStep? owner, Occurrence? occurrence,
        bool effectProcedure = false)
    {
        var captured = execution.Capture(cast, abilityOrdinal);
        var capture = effectProcedure ? AbilityContinuationScheduling.ForEffectProcedure(captured)
            : AbilityContinuationScheduling.ForCostProcedure(captured);
        var continuation = AbilityContinuations.Step(
            capture, AbilityContinuationStage.Resume, cast.World.Agenda.Current?.Round ?? 0, plan: true);
        if (owner is null)
        {
            cast.World.Agenda.Then(continuation);
        }
        else
        {
            cast.World.Agenda.ContinueBeforeOwner(
                occurrence ?? throw new InvalidOperationException(
                    "a suspended cost has no containing occurrence"),
                owner.Value, continuation);
        }
    }

    internal static AbilityContinuationState Capture(this AbilityResolutionExecution execution,
        AbilityResolutionState cast, int ordinal, AbilityCardReference? chosen = null,
        IReadOnlyList<AbilityStructuralFrame>? frames = null)
    {
        var ability = execution.AbilityAt(cast.Source, cast.Tier, ordinal, cast.AbilityFace);
        var crisis = AbilityContinuations.CrisisIgnoringThwartOrdinals(
            ability, cast.ValidatedCrisisIgnoringThwarts, cast.RestoredCrisisIgnoringThwarts);
        var selected = chosen ?? cast.CaptureCurrentSelection();
        var binding = selected is null ? null : new AbilityContinuationCardBinding(
            selected.Card.ObjectId, selected.Area, selected.Incarnation);
        return new AbilityContinuationState(
            new AbilityInvocation(new(cast.AbilityFace, cast.Tier, ordinal),
                new(cast.Source.ObjectId, cast.Source.Area.Id, cast.SourceBindingIncarnation),
                cast.Occurrence, cast.Trigger, cast.AbilityPlayer, cast.AbilityActor?.ObjectId ?? -1)
                { TriggeringAttack = cast.TriggeringAttack, TriggeringDefeat = cast.TriggeringDefeat,
                    TriggeringThreat = cast.TriggeringThreat, DamageOrigin = cast.DamageOrigin },
            new AbilityCursor([.. frames ?? cast.StructuralPath], cast.Position, cast.Player,
                cast.HasContinuation, cast.FinalStep, cast.FinalPlayer, cast.EachPlayerFrame),
            new AbilityMemory(cast.Results.ToImmutableDictionary(StringComparer.Ordinal),
                [.. cast.Discarded.Select(card => card.ObjectId)], binding, crisis,
                cast.CaptureSourceState(), cast.GainedKeywords.Contains("surge"))
            { ExhaustedForCost = cast.ExhaustedForCost.ToImmutableHashSet(), DamageDefeats = cast.DamageDefeats,
                SourceWasPublicInPlay = cast.SourceWasPublicInPlay })
        { Execution = cast.Execution, ThreatMovePairs = cast.ThreatMovePairs, ThreatMove = cast.PendingThreatMove, ThreatMoveRequested = cast.ThreatMoveRequested, Completion = cast.PendingCompletion, DamageCompletion = cast.PendingDamageCompletion,
            CompletedOutcome = cast.CompletedOutcome, Payment = cast.PendingPayment, DamageReplacement = cast.DamageReplacement,
            PowerExecution = cast.PowerExecution is { } power
                ? power with { Attacked = [.. cast.Attacked.Select(card => card.ObjectId)] } : null };

    }

}
