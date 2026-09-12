using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionInterpreter
{
    private static readonly HashSet<Type> DirectStructuralTransitionTypes =
    [
        typeof(StartSequenceCommand), typeof(StartDependentCommand),
        typeof(StartForEachCommand), typeof(StartEachTimeCommand),
        typeof(SchedulePowerCommand), typeof(RunDefenseCommand),
        typeof(ScheduleActivationsCommand), typeof(RunLeaf), typeof(Ask),
    ];

    /// <summary>The one player allowed to use this encounter-card ability, if any.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:ability.8.1</c>: only the controller of a player card bearing an
    /// attachment may trigger or pay for that attachment's abilities that use
    /// “you” or “your”. The host supplies that controller; the attachment is
    /// still owned by the scenario.
    /// </para>
    /// <para>
    /// <c>rr:ability.8.2</c>: only the player whose play area holds an
    /// obligation may trigger or pay for it. This is a permission distinct
    /// from control, because an obligation remains an encounter card.
    /// </para>
    /// </remarks>
    internal static int? RestrictedPlayer(this AbilityResolutionExecution execution, World world, CompiledCardAbility ability, Card card)
    {
        // The Golden Rules give explicit card text precedence. Obedience
        // Potion-shaped attachments say “Any player can do this,” so that
        // permission overrides the otherwise card-wide “your identity” binding.
        if (ability.AnyPlayer)
        {
            return null;
        }

        if (world.Facts.Kind(card.FaceId) == CardKind.Obligation
            && card.Area.PlayArea.IsPlayers)
        {
            return card.Area.PlayArea.Player;
        }

        if (world.Facts.Kind(card.FaceId) == CardKind.Attachment
            && card.Area.Host >= 0
            && card.Area.Host < world.Cards.Count
            && execution.UsesYouOrYour(ability, card))
        {
            int controller = execution.ControllerOf(world, world.Cards[card.Area.Host]);
            return controller >= 0 ? controller : null;
        }

        return null;
    }

    /// <summary>Whether the authored ability contains the printed “you/your” binding.</summary>
    internal static bool UsesYouOrYour(this AbilityResolutionExecution execution, CompiledCardAbility ability, Card card) =>
        AbilityPlayerBindingAnalysis.UsesYouOrYour(execution.program, ability, card);

    internal static bool ContainsYouOrYour(this AbilityResolutionExecution execution, AbilityCost? cost) =>
        AbilityPlayerBindingAnalysis.Contains(cost);

    internal static bool Subject(this AbilityResolutionExecution execution,
        World world, string? subject, Card card, Occurrence occurrence,
        int? restricted = null) => subject switch
        {
            null => true,
            AbilitySubjects.This => occurrence.Subject == card.ObjectId,
            AbilitySubjects.AttachedTo => card.Area.Host >= 0 && occurrence.Subject == card.Area.Host,
            AbilitySubjects.You => occurrence.Player >= 0
                && occurrence.Player == (restricted ?? execution.ControllerOf(world, card)),

            // Nothing to match: the condition alone decides. `Waiting` has already
            // checked that the card is in play and that the occurrence carries the
            // condition, which is the whole of what such a card asks for.
            AbilitySubjects.Game => true,
            _ => throw new AbilityException($"'{subject}' is not a subject anything matches"),
        };

    /// <summary>Whether a captured card fills one named occurrence role.</summary>
    internal static bool Role(this AbilityResolutionExecution execution,
        World world, string? match, Card card, OccurrenceCard? role,
        int? restricted = null) => match switch
        {
            null => true,
            _ when role is null => false,
            AbilityRoles.This => role.Card == card.ObjectId,
            AbilityRoles.AttachedTo => card.Area.Host >= 0 && role.Card == card.Area.Host,
            AbilityRoles.You => role.Controller >= 0
                && (restricted is { } player
                    ? role.Controller == player
                    : card.Owner == World.Scenario || role.Controller == execution.ControllerOf(world, card)),
            AbilityRoles.Villain => role.IsVillain,
            AbilityRoles.Minion => role.IsMinion,
            AbilityRoles.Hero => role.IsHero,
            AbilityRoles.Ally => role.IsAlly,
            AbilityRoles.Friendly => role.IsFriendly,
            AbilityRoles.Enemy => role.IsEnemy,
            _ => throw new AbilityException($"'{match}' is not an occurrence role matcher"),
        };

    /// <summary>Whether the occurrence's player fills the trigger's player role.</summary>
    internal static bool Player(this AbilityResolutionExecution execution,
        World world, string? match, Card card, Occurrence occurrence,
        int? restricted = null) => match switch
        {
            null or AbilityPlayers.TriggerPlayer => true,
            AbilityPlayers.You => occurrence.Player >= 0
                && occurrence.Player == (restricted ?? execution.ControllerOf(world, card)),
            _ => throw new AbilityException($"'{match}' is not an occurrence player matcher"),
        };

    // ---- the effect tree ---------------------------------------------------

    internal static void Run(this AbilityResolutionExecution execution, CompiledCardAbility ability, AbilityResolutionState cast)
    {
        var labels = ability.Labels;
        if (labels.Length > 0)
        {
            if (!cast.LabelsPreflighted)
            {
                if (!AbilityInitiation.LabelsCanInitiate(ability, execution.AdmissionContext(cast)))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' cannot initiate its labeled ability "
                        + "in the current state");
                }
                cast.LabelsPreflighted = true;
            }

            var performer = LabeledAbilities.Begin(
                cast.World, cast.World.Facts, execution.Resolver(cast), cast.Source,
                labels, cast.Events);
            if (performer is null)
            {
                return;
            }

            cast.AbilityActor = performer;
            if (labels.Contains(Attack.DefenseVerb, StringComparer.Ordinal))
            {
                cast.Results["defenseAbilityDefender"] = performer.ObjectId;
                Attack.BeginDefenseAbility(cast.World, execution.Resolver(cast), performer);
            }
        }

        execution.Run(ability.Effect, cast);
    }

    internal static void Run(this AbilityResolutionExecution execution, AbilityEffect node, AbilityResolutionState cast)
    {
        int eventsBefore = cast.Events.Count;
        var agendaOwner = cast.World.Agenda.Current;
        var agendaOccurrence = cast.World.Agenda.Occurrence;
        var healthBefore = cast.World.Effects.CaptureCharacterHealth();
        var instruction = node;
        if (!execution.TryRunCardState(instruction, cast)
            && !execution.TryRunImmediateEffect(instruction, cast)
            && !execution.TryRunDamageAndThreat(instruction, node, cast)
            && !execution.TryRunCardMovement(instruction, cast))
        {
            execution.RunRemainingEffect(node, cast);
        }
        if (cast.Events.Count > eventsBefore
            && AbilityStructuralExecution.EventMeansEffectApplied(node))
        {
            cast.ResolveEffect();
        }

        // A conditional constant can become Stalwart because this node changed
        // threat, counters, traits, or another dependency. `rr:stalwart.2`
        // removes existing stunned/confused cards at that transition, before
        // later text in the same ability reads the board.
        Statuses.RemoveAfflictionsIfStalwart(
            cast.World, cast.World.Facts, "stalwart", cast.Events);
        bool healthDefeatSuspended = cast.World.Effects.SettleLostHealth(
            healthBefore, cast.Trigger, cast.Events);
        if (healthDefeatSuspended && !cast.Suspended)
        {
            execution.SuspendAfterProcedure(
                node, cast, agendaOwner, agendaOccurrence);
        }

        // `rr:attack-enemy-activation.3.2`: a defending ally that leaves play
        // immediately stops defending and exposes its controller's identity.
        // Recheck after every node so later text in the same ability, and the
        // next boost ability, reads the new attack roles rather than a stale
        // defender that has already moved.
        Attack.RefreshDefender(cast.World, cast.World.Facts);
    }

    internal static void RunRemainingEffect(this AbilityResolutionExecution execution, AbilityEffect node, AbilityResolutionState cast)
        => execution.ApplyStructuralDecision(AbilityStructuralExecution.Decide(
            execution.StructuralContext(cast), node), cast);

    internal static void RunDefense(this AbilityResolutionExecution execution, RunDefenseCommand command, AbilityResolutionState cast)
    {
        var defender = cast.AbilityActor ?? LabeledAbilities.Begin(
            cast.World, cast.World.Facts, execution.Resolver(cast), cast.Source,
            [Attack.DefenseVerb], cast.Events);
        if (defender is null)
            return;
        if (cast.AbilityActor is null)
        {
            cast.Results["defenseAbilityDefender"] = defender.ObjectId;
            Attack.BeginDefenseAbility(cast.World, execution.Resolver(cast), defender);
        }
        execution.RunChild(command.Effect.Effect, new DefenseFrame(), cast);
    }

    // The owner has decided the structural transition. This trampoline performs
    // only its explicit domain command or continuation suspension.
    internal static void ApplyStructuralDecision(this AbilityResolutionExecution execution, AbilityStructuralTransition transition, AbilityResolutionState cast)
    {
        if (DirectStructuralTransitionTypes.Contains(transition.GetType()))
        {
            execution.ApplyDirectStructuralDecision(transition, cast);
            return;
        }
        switch (transition)
        {
            case RunChoice choice:
                execution.ApplyRunChoice(choice, cast);
                return;
            case ScheduleEachPlayer schedule:
                execution.ApplyScheduleEachPlayer(schedule, cast);
                return;
            case DelayAfterActivation delay:
                execution.ApplyDelayAfterActivation(delay, cast);
                return;
            case RunOrdered ordered:
                execution.ApplyRunOrdered(ordered, cast);
                return;
            case Complete complete:
                execution.ApplyStructuralCompletion(complete, cast);
                return;
            case Rejected rejected:
                throw new AbilityException(rejected.Reason);
            case Unsupported unsupported:
                throw new RulesNotImplementedException(unsupported.Reason);
            default:
                throw new InvalidOperationException(
                    $"Structural execution stopped at {transition.GetType().Name}");
        }
    }

    private static void ApplyDirectStructuralDecision(
        this AbilityResolutionExecution execution,
        AbilityStructuralTransition transition, AbilityResolutionState cast)
    {
        switch (transition)
        {
            case StartSequenceCommand sequence:
                execution.Sequence(sequence.Effect, cast, from: 0);
                break;
            case StartDependentCommand dependent:
                execution.ResolveDependent(dependent.Effect, cast);
                break;
            case StartForEachCommand repeated:
                execution.ForEach(repeated.Effect, cast);
                break;
            case StartEachTimeCommand repeated:
                execution.EachTime(repeated.Effect, cast);
                break;
            case SchedulePowerCommand power:
                execution.SchedulePower(power, cast);
                break;
            case RunDefenseCommand defense:
                execution.RunDefense(defense, cast);
                break;
            case ScheduleActivationsCommand activations:
                execution.ScheduleActivations(activations, cast);
                break;
            case RunLeaf leaf:
                execution.RunStructuralLeaf(leaf, cast);
                break;
            case Ask ask:
                execution.SuspendForChoice(ask.Choice, cast);
                break;
            default:
                throw new InvalidOperationException("Unknown direct structural transition");
        }
    }

    private static void ApplyRunChoice(
        this AbilityResolutionExecution execution, RunChoice choice,
        AbilityResolutionState cast)
    {
        _ = execution.ApplyAdmission(choice.Admission, cast);
        if (choice.BindsPlayerSelection) cast.ChooseSelection(choice.Selection);
        if (choice.PendingOutcome is { } outcome)
            cast.CompletePendingDependency((ResolutionOutcome)(int)outcome);
        execution.RunStructuralLeaf(new RunLeaf(
            choice.Effect, [.. cast.StructuralPath, choice.Frame],
            cast.Position, cast.HasContinuation), cast);
    }

    private static void ApplyScheduleEachPlayer(
        this AbilityResolutionExecution execution, ScheduleEachPlayer schedule,
        AbilityResolutionState cast)
    {
        int ordinal = execution.AbilityOrdinal(schedule.Effect, cast);
        EachPlayerEffects.Schedule(cast.World, AbilityContinuationCodec.Step(
            execution.Capture(cast, ordinal), Steps.ResolveEachPlayer,
            cast.World.Agenda.Current?.Round ?? 0));
        cast.Suspend();
    }

    private static void ApplyDelayAfterActivation(
        this AbilityResolutionExecution execution, DelayAfterActivation delay,
        AbilityResolutionState cast)
    {
        var activation = cast.World.Activation
            ?? throw new InvalidOperationException("Structural owner admitted no activation");
        execution.runtimes.AfterActivation(
            cast.World, activation.Id, new ActivationEffect(
                cast.Source.ObjectId, cast.Player, cast.Tier, delay.Effect.Effect,
                cast.Altered?.ObjectId ?? -1, cast.AbilityActor?.ObjectId ?? -1));
        cast.ResolveEffect();
    }

    private static void ApplyRunOrdered(
        this AbilityResolutionExecution execution, RunOrdered ordered,
        AbilityResolutionState cast)
    {
        for (int position = 0; position < ordered.Effects.Length; position++)
        {
            var frame = ordered.Frames[position];
            cast.StructuralPath.Add(frame);
            try
            {
                cast.SetContinuation(
                    cast.HasContinuation || position < ordered.Effects.Length - 1);
                execution.Run(ordered.Effects[position], cast);
            }
            finally
            {
                cast.StructuralPath.RemoveAt(cast.StructuralPath.Count - 1);
            }
            if (cast.Suspended) return;
        }
    }

    /// <summary>
    /// "Put the top card of your deck into play facedown … as a Drone minion."
    /// </summary>
    internal static bool CanCreateDrones(this AbilityResolutionExecution execution, AbilityEffect node, AbilityResolutionState cast) =>
        execution.EffectOf<AbilityEffect.CreateDrones>(node, cast) is var drones
        && AbilityAdmissionFacts.CanCreateDrones(
            cast.World, drones.Players switch
            {
                AbilityPlayerSelection.AllPlayers => cast.World.PlayerOrder,
                AbilityPlayerSelection.OnePlayer one => [execution.Seat(one.Player, cast)],
                _ => throw new InvalidOperationException("Unknown compiled player selection"),
            }, drones.Count);

}
