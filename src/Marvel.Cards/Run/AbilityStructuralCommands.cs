using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Applies the structural owner's explicit commands and suspension transitions.</summary>
internal static class AbilityStructuralCommands
{
    private static readonly HashSet<Type> DirectStructuralTransitionTypes =
    [
        typeof(StartSequenceCommand), typeof(StartDependentCommand),
        typeof(StartForEachCommand), typeof(StartEachTimeCommand),
        typeof(SchedulePowerCommand), typeof(RunDefenseCommand),
        typeof(ScheduleActivationsCommand), typeof(RunLeaf), typeof(Ask), typeof(NextSpecialCommand),
    ];

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
        if (execution.TryStartStructuralFrame(transition, cast)) return;
        switch (transition)
        {
            case SchedulePowerCommand power:
                execution.SchedulePower(power, cast);
                break;
            case RunDefenseCommand defense:
                execution.RunDefense(defense, cast);
                break;
            case NextSpecialCommand special:
                execution.Apply(special, cast);
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

    private static bool TryStartStructuralFrame(
        this AbilityResolutionExecution execution,
        AbilityStructuralTransition transition, AbilityResolutionState cast)
    {
        switch (transition)
        {
            case StartSequenceCommand sequence:
                execution.Sequence(sequence.Effect, cast, from: 0);
                return true;
            case StartDependentCommand dependent:
                execution.ResolveDependent(dependent.Effect, cast);
                return true;
            case StartForEachCommand repeated:
                execution.ForEach(repeated.Effect, cast);
                return true;
            case StartEachTimeCommand repeated:
                execution.EachTime(repeated.Effect, cast);
                return true;
            default:
                return false;
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

}
