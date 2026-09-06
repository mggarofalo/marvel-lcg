using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
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
    private int? RestrictedPlayer(World world, CompiledCardAbility ability, Card card)
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
            && UsesYouOrYour(ability, card))
        {
            int controller = ControllerOf(world, world.Cards[card.Area.Host]);
            return controller >= 0 ? controller : null;
        }

        return null;
    }

    /// <summary>Whether the authored ability contains the printed “you/your” binding.</summary>
    private bool UsesYouOrYour(CompiledCardAbility ability, Card card) =>
        AbilityPlayerBindingAnalysis.UsesYouOrYour(program, ability, card);

    private static bool ContainsYouOrYour(AbilityCost? cost) =>
        AbilityPlayerBindingAnalysis.Contains(cost);

    private static bool Subject(
        World world, string? subject, Card card, Occurrence occurrence,
        int? restricted = null) => subject switch
    {
        null => true,
        AbilitySubjects.This => occurrence.Subject == card.ObjectId,
        AbilitySubjects.AttachedTo => card.Area.Host >= 0 && occurrence.Subject == card.Area.Host,
        AbilitySubjects.You => occurrence.Player >= 0
            && occurrence.Player == (restricted ?? ControllerOf(world, card)),

        // Nothing to match: the condition alone decides. `Waiting` has already
        // checked that the card is in play and that the occurrence carries the
        // condition, which is the whole of what such a card asks for.
        AbilitySubjects.Game => true,
        _ => throw new AbilityException($"'{subject}' is not a subject anything matches"),
    };

    /// <summary>Whether a captured card fills one named occurrence role.</summary>
    private static bool Role(
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
                : card.Owner == World.Scenario || role.Controller == ControllerOf(world, card)),
        AbilityRoles.Villain => role.IsVillain,
        AbilityRoles.Minion => role.IsMinion,
        AbilityRoles.Hero => role.IsHero,
        AbilityRoles.Ally => role.IsAlly,
        AbilityRoles.Friendly => role.IsFriendly,
        AbilityRoles.Enemy => role.IsEnemy,
        _ => throw new AbilityException($"'{match}' is not an occurrence role matcher"),
    };

    /// <summary>Whether the occurrence's player fills the trigger's player role.</summary>
    private static bool Player(
        World world, string? match, Card card, Occurrence occurrence,
        int? restricted = null) => match switch
    {
        null or AbilityPlayers.TriggerPlayer => true,
        AbilityPlayers.You => occurrence.Player >= 0
            && occurrence.Player == (restricted ?? ControllerOf(world, card)),
        _ => throw new AbilityException($"'{match}' is not an occurrence player matcher"),
    };

    // ---- the effect tree ---------------------------------------------------

    private void Run(CompiledCardAbility ability, AbilityResolutionState cast)
    {
        var labels = ability.Labels;
        if (labels.Length > 0)
        {
            if (!cast.LabelsPreflighted)
            {
                if (!AbilityInitiation.LabelsCanInitiate(ability, AdmissionContext(cast)))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' cannot initiate its labeled ability "
                        + "in the current state");
                }
                cast.LabelsPreflighted = true;
            }

            var performer = LabeledAbilities.Begin(
                cast.World, cast.World.Facts, Resolver(cast), cast.Source,
                labels, cast.Events);
            if (performer is null)
            {
                return;
            }

            cast.AbilityActor = performer;
            if (labels.Contains(Attack.DefenseVerb, StringComparer.Ordinal))
            {
                cast.Results["defenseAbilityDefender"] = performer.ObjectId;
                Attack.BeginDefenseAbility(cast.World, Resolver(cast), performer);
            }
        }

        Run(ability.Effect, cast);
    }

    private void Run(AbilityEffect node, AbilityResolutionState cast)
    {
        int eventsBefore = cast.Events.Count;
        var agendaOwner = cast.World.Agenda.Current;
        var agendaOccurrence = cast.World.Agenda.Occurrence;
        var healthBefore = cast.World.Effects.CaptureCharacterHealth();
        var instruction = node;
        if (!TryRunCardState(instruction, cast)
            && !TryRunImmediateEffect(instruction, cast)
            && !TryRunDamageAndThreat(instruction, node, cast)
            && !TryRunCardMovement(instruction, cast))
        {
            RunRemainingEffect(node, cast);
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
            SuspendAfterProcedure(
                node, cast, agendaOwner, agendaOccurrence);
        }

        // `rr:attack-enemy-activation.3.2`: a defending ally that leaves play
        // immediately stops defending and exposes its controller's identity.
        // Recheck after every node so later text in the same ability, and the
        // next boost ability, reads the new attack roles rather than a stale
        // defender that has already moved.
        Attack.RefreshDefender(cast.World, cast.World.Facts);
    }

    private void RunRemainingEffect(AbilityEffect node, AbilityResolutionState cast)
        => ApplyStructuralDecision(AbilityStructuralExecution.Decide(
            StructuralContext(cast), node), cast);

    private void RunDefense(RunDefenseCommand command, AbilityResolutionState cast)
    {
        var defender = cast.AbilityActor ?? LabeledAbilities.Begin(
            cast.World, cast.World.Facts, Resolver(cast), cast.Source,
            [Attack.DefenseVerb], cast.Events);
        if (defender is null)
            return;
        if (cast.AbilityActor is null)
        {
            cast.Results["defenseAbilityDefender"] = defender.ObjectId;
            Attack.BeginDefenseAbility(cast.World, Resolver(cast), defender);
        }
        RunChild(command.Effect.Effect, new DefenseFrame(), cast);
    }

    // The owner has decided the structural transition. This trampoline performs
    // only its explicit domain command or continuation suspension.
    private void ApplyStructuralDecision(AbilityStructuralTransition transition, AbilityResolutionState cast)
    {
        switch (transition)
        {
            case StartSequenceCommand sequence:
                Sequence(sequence.Effect, cast, from: 0);
                return;
            case StartDependentCommand dependent:
                ResolveDependent(dependent.Effect, cast);
                return;
            case StartForEachCommand repeated:
                ForEach(repeated.Effect, cast);
                return;
            case StartEachTimeCommand repeated:
                EachTime(repeated.Effect, cast);
                return;
            case SchedulePowerCommand power:
                SchedulePower(power, cast);
                return;
            case RunDefenseCommand defense:
                RunDefense(defense, cast);
                return;
            case ScheduleActivationsCommand activations:
                ScheduleActivations(activations, cast);
                return;
            case RunLeaf leaf:
                RunStructuralLeaf(leaf, cast);
                return;
            case RunChoice choice:
                _ = ApplyAdmission(choice.Admission, cast);
                if (choice.BindsPlayerSelection)
                    cast.ChooseSelection(choice.Selection);
                if (choice.PendingOutcome is { } outcome)
                    cast.CompletePendingDependency((ResolutionOutcome)(int)outcome);
                RunStructuralLeaf(new RunLeaf(
                    choice.Effect, [.. cast.StructuralPath, choice.Frame],
                    cast.Position, cast.HasContinuation), cast);
                return;
            case Ask ask:
                SuspendForChoice(ask.Choice, cast);
                return;
            case ScheduleEachPlayer schedule:
                int ordinal = AbilityOrdinal(schedule.Effect, cast);
                EachPlayerEffects.Schedule(cast.World, AbilityContinuationCodec.Step(
                    Capture(cast, ordinal), Steps.ResolveEachPlayer,
                    cast.World.Agenda.Current?.Round ?? 0));
                cast.Suspend();
                return;
            case DelayAfterActivation delay:
                var activation = cast.World.Activation
                    ?? throw new InvalidOperationException("Structural owner admitted no activation");
                runtimes.AfterActivation(
                    cast.World, activation.Id, new ActivationEffect(
                        cast.Source.ObjectId, cast.Player, cast.Tier, delay.Effect.Effect,
                        cast.Altered?.ObjectId ?? -1, cast.AbilityActor?.ObjectId ?? -1));
                cast.ResolveEffect();
                return;
            case RunOrdered ordered:
                for (int position = 0; position < ordered.Effects.Length; position++)
                {
                    var frame = ordered.Frames[position];
                    cast.StructuralPath.Add(frame);
                    try
                    {
                        cast.SetContinuation(cast.HasContinuation || position < ordered.Effects.Length - 1);
                        Run(ordered.Effects[position], cast);
                    }
                    finally { cast.StructuralPath.RemoveAt(cast.StructuralPath.Count - 1); }
                    if (cast.Suspended) return;
                }
                return;
            case Complete complete:
                ApplyStructuralCompletion(complete, cast);
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

    /// <summary>
    /// "Put the top card of your deck into play facedown … as a Drone minion."
    /// </summary>
    private bool CanCreateDrones(AbilityEffect node, AbilityResolutionState cast) =>
        EffectOf<AbilityEffect.CreateDrones>(node, cast) is var drones
        && AbilityAdmissionFacts.CanCreateDrones(
            cast.World, drones.Players switch
            {
                AbilityPlayerSelection.AllPlayers => cast.World.PlayerOrder,
                AbilityPlayerSelection.OnePlayer one => [Seat(one.Player, cast)],
                _ => throw new InvalidOperationException("Unknown compiled player selection"),
            }, drones.Count);

}
