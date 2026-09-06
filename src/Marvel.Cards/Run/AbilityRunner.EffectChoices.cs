using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    /// <summary>Suspend an ability for one persisted player choice.</summary>
    private static void SuspendForChoice(AbilityEffect node, Cast cast)
    {
        // `Index` remains the legacy top-level resume point. New continuations
        // use AbilityOrdinal and AbilityPath below.
        int abilityOrdinal = AbilityOrdinal(node, cast);
        var continuation = AbilityContinuationCodec.Step(
            Capture(cast, abilityOrdinal), Steps.ChooseOption,
            cast.World.Agenda.Current?.Round ?? 0);
        if (cast.Occurrence.Is(Steps.TurnAction))
        {
            cast.World.Agenda.ThenContinuation(continuation, cast.Occurrence);
        }
        else
        {
            cast.World.Agenda.Then(continuation);
        }

        cast.Suspend();
    }

    private static IReadOnlyList<Card> DamageTargets(AbilityCardSelection targets, Cast cast) =>
        AbilityDamageAndThreatExecution.DamageTargets(targets, DamageAndThreatContext(cast));

    /// <summary>Deals an assignment that is already worked out.</summary>
    /// <remarks>
    /// In object-id order, because <c>rr:indirect-damage.3</c> resolves it
    /// "simultaneously" and simultaneous still has to reach the event stream in
    /// some order — one the board cannot see and the wire can.
    /// </remarks>
    private static void Resolve(
        AbilityEffect node, Cast cast, Dictionary<int, long> assigned)
        => ApplyDamageAndThreat(
            AbilityDamageAndThreatExecution.ResolveAssigned(
                node, assigned, DamageAndThreatContext(cast)),
            node, cast);

    /// <summary>"Deal N damage to …" — <c>rr:damage</c>.</summary>
    /// <remarks>
    /// Through <see cref="Damage.Deal"/> and not at the token, because damage
    /// is one rule however it arrived: <c>rr:tough.2</c> prevents all of it and
    /// discards a status card instead, and <c>rr:defeat</c> is the other half
    /// of the same moment. A card that wrote to <c>k_damage</c> would skip
    /// both and leave a defeated character standing.
    /// </remarks>
    private static void DealDamage(AbilityEffect.Damage damage, AbilityEffect node, Cast cast, long multiplier = 1)
        => ApplyDamageAndThreat(
            AbilityDamageAndThreatExecution.DealDamage(
                damage, node, DamageAndThreatContext(cast), multiplier),
            node, cast);

    private static void SchedulePower(SchedulePowerCommand command, Cast cast)
    {
        var continuationChosen = cast.CaptureCurrentSelection();
        cast.Choose(command.Target);
        var capture = Capture(cast, command.AbilityIndex, continuationChosen);
        var continuation = AbilityContinuationCodec.Power(
            capture, command.PowerOrdinal, cast.HasContinuation);
        bool scheduled = command.Verb == BasicPowers.AttackVerb
            ? BasicPowers.CardAttack(
                cast.World, cast.World.Facts, Resolver(cast), cast.Source,
                command.Target, command.Amount,
                cast.Trigger, cast.Events, continuation,
                [.. command.Targets.Select(card => card.ObjectId)], cast.AbilityActor)
            : BasicPowers.CardThwart(
                cast.World, cast.World.Facts, Resolver(cast), cast.Source,
                command.Target, command.Amount,
                cast.Trigger, cast.Events, continuation,
                [.. command.Targets.Select(card => card.ObjectId)], cast.Occurrence.Threat,
                command.AutomaticThwartTarget, cast.AbilityActor);
        if (!scheduled)
        {
            return;
        }

        cast.Suspend();
    }

    private static void RemoveThreat(AbilityEffect.RemoveThreat removal, Cast cast, long multiplier = 1)
        => ApplyDamageAndThreat(
            AbilityDamageAndThreatExecution.RemoveThreat(
                removal, DamageAndThreatContext(cast), multiplier),
            removal, cast);

    private static long EventModifier(Cast cast, string kind) =>
        AbilityEventModifiers.Amount(cast.World, cast.Source, kind);

    private static IReadOnlyList<ContinuousEffect> EventModifierEffects(
        Cast cast, string kind)
        => AbilityEventModifiers.Effects(cast.World, cast.Source, kind);

    /// <summary>Which card type a word names.</summary>
    private static CardKind Kind(string named) => named switch
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
    /// <b>Scheduled, not called.</b> <c>rr:attack-enemy-activation</c> is six
    /// steps and one of them asks a player who is defending, so an activation
    /// cannot resolve inside an ability that has to return. It goes on the
    /// agenda, and <c>Agenda.Then</c> puts it after the step that is running —
    /// which is what <c>rr:surge.2</c> wants anyway: finish resolving the card
    /// before what it caused happens.
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
    private static void ScheduleActivations(
        ScheduleActivationsCommand command, Cast cast)
    {
        int round = cast.World.Agenda.Current?.Round ?? 0;
        var activations = new List<PhaseStep>();
        foreach (var target in command.Targets)
        {
            activations.Add(new PhaseStep(
                command.Effect.Attack ? Steps.Attack : Steps.Scheme,
                round, 2, Index: target.Seat, Subject: target.Enemy.ObjectId,
                Seat: target.Seat, Character: command.Against));
        }

        var activationIds = new List<int>();
        foreach (var activation in activations)
        {
            if (command.Dynamic)
            {
                cast.Results[$"dynamicActivation:{activation.Subject}"] = 1;
            }
            if (command.First)
            {
                activationIds.Add(cast.World.Agenda.NowActivation(activation));
            }
            else
            {
                activationIds.Add(cast.World.Agenda.ThenActivation(activation));
            }
        }

        if (activationIds.Count > 0)
        {
            int abilityOrdinal = AbilityOrdinal(command.Effect, cast);
            var capture = AbilityContinuationCodec.ForActivations(
                Capture(cast, abilityOrdinal), command.Dynamic);
            cast.World.Agenda.AfterActivations(activationIds, AbilityContinuationCodec.Step(
                capture, Steps.ResumeAbility, round, plan: true, activationIds: activationIds));
            cast.WaitFor(activationIds);
            cast.Suspend();
        }
        else if (command.Dynamic)
        {
            AbilityContinuationCodec.CompleteDynamicActivation(cast.Results);
        }
    }

    /// <summary>Resume the containing ability after a rules procedure finishes.</summary>
    private static void SuspendAfterProcedure(
        AbilityEffect node, Cast cast, PhaseStep? agendaOwner = null,
        Occurrence? agendaOccurrence = null)
    {
        int abilityOrdinal = AbilityOrdinal(node, cast);
        var capture = AbilityContinuationCodec.ForEffectProcedure(
            Capture(cast, abilityOrdinal));
        var continuation = AbilityContinuationCodec.Step(
            capture, Steps.ResumeAbility, cast.World.Agenda.Current?.Round ?? 0, plan: true);
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
    private static void SuspendAfterCost(
        Cast cast, int abilityOrdinal, PhaseStep? owner, Occurrence? occurrence)
    {
        var capture = AbilityContinuationCodec.ForCostProcedure(
            Capture(cast, abilityOrdinal));
        var continuation = AbilityContinuationCodec.Step(
            capture, Steps.ResumeAbility, cast.World.Agenda.Current?.Round ?? 0, plan: true);
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

    private static AbilityContinuationCapture Capture(
        Cast cast, int ordinal, AbilityCardReference? chosen = null,
        IReadOnlyList<AbilityStructuralFrame>? frames = null)
    {
        var results = new Dictionary<string, long>(cast.Results, StringComparer.Ordinal);
        var runner = (AbilityRunner)cast.Abilities;
        var ability = runner.AbilityAt(
            cast.Source, cast.Tier, ordinal, cast.AbilityFace);
        var crisis = AbilityContinuationCodec.CrisisIgnoringThwartOrdinals(
            ability, cast.ValidatedCrisisIgnoringThwarts,
            cast.RestoredCrisisIgnoringThwarts);
        var selection = chosen ?? cast.CaptureCurrentSelection();
        var binding = selection is null ? null : new AbilityContinuationCardBinding(
            selection.Card.ObjectId, selection.Area, selection.Incarnation);
        return AbilityContinuationCodec.Capture(
            cast.Source.ObjectId, cast.SourceBindingIncarnation,
            new AbilityContinuationAddress(cast.AbilityFace, cast.Tier, ordinal),
            frames ?? cast.StructuralPath, cast.Position, cast.Player, cast.AbilityPlayer,
            cast.AbilityActor?.ObjectId ?? -1, cast.FinalStep, cast.FinalPlayer,
            cast.EachPlayerFrame, cast.HasContinuation, cast.Trigger,
            cast.GainedKeywords.Contains("surge"), cast.Occurrence,
            cast.Discarded.Select(card => card.ObjectId), results, binding, crisis);
    }

    /// <summary>Propagate one reveal-scoped Surge gain to work already suspended.</summary>
    private static void RememberGainedSurge(World world, int source)
    {
        // Choice and each-player continuations are saveable agenda data. An
        // earlier ability can already have scheduled one when a later sibling
        // ability gains Surge, so its original snapshot must be advanced too.
        // The rulebook determines the shared non-numeric keyword instance; the
        // propagation mechanism is the engine's choice.
        world.Agenda.MarkSurgeGained(source);
        if (world.CharacterAttack is { Source: var attackSource } attack
            && attackSource == source)
        {
            world.CharacterAttack = attack with { SurgeGained = true };
        }
        if (world.CharacterThwart is { Source: var thwartSource } thwart
            && thwartSource == source)
        {
            world.CharacterThwart = thwart with { SurgeGained = true };
        }
    }

}
