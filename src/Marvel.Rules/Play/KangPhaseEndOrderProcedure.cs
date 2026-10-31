using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>The first player's order for Kang's delayed effect and others due at one phase ending.</summary>
/// <remarks>
/// <para>
/// Kang I's and Kang II's departures are delayed effects that resolve "at the end
/// of the phase" (11001, 11002-11005). Another delayed effect can come due at
/// the same ending, and <c>rr:first-player.3</c> settles who orders them: "If two
/// or more effects would resolve simultaneously, the first player decides the
/// order in which to resolve them." MARVEL-894 Q6 applies it to phase-end effects.
/// </para>
/// <para>
/// The engine offers the order as a choice between two blocks — Kang's effect,
/// which may itself ask further questions, or every other due delayed effect,
/// which resolve in registration order as they do at any phase ending. That
/// grouping is the engine's choice. When Kang's goes first, the other effects
/// wait in a frame that moves behind every frame Kang's resolution adds before
/// the phase-ending step, so they resolve only once Kang's has completely
/// resolved.
/// </para>
/// </remarks>
internal static class KangPhaseEndOrderProcedure
{
    private const int KangFirst = 0;
    private const int OthersFirst = 1;

    /// <summary>Whether Kang's delayed effect and another delayed effect are both due at this step.</summary>
    internal static bool Due(World world, PhaseStep step) =>
        (world.Agenda.KangSplit.IsBoundary(step) || world.Agenda.KangTransitions.IsBoundary(step))
        && world.Effects.HasDue(StepConditions.ConditionsOf(step.What));

    internal static void Schedule(World world, PhaseStep owner)
    {
        var origin = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("the Kang phase ending has no occurrence");
        world.Agenda.ContinueBeforeOwner(origin, owner,
            owner with { Operation = new KangPhaseEndOrderOperation(owner, origin) });
    }

    internal static Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        var operation = step.Require<KangPhaseEndOrderOperation>();
        if (!operation.Deferred) return Ask(world);
        // Kang's resolution keeps adding frames before the owner (a departure's
        // join, the split's stage reveals), and each lands behind this one. Move
        // behind them until nothing but the owner remains.
        if (world.Agenda.HasWorkBeforeOwner(step, operation.Owner, operation.Origin))
        {
            world.Agenda.ContinueBeforeOwner(operation.Origin, operation.Owner, step);
            return null;
        }
        ResolveOthers(world, operation.Owner, events);
        return null;
    }

    internal static void Answer(World world, PhaseStep step, Decision input, List<GameEvent> events)
    {
        var operation = step.Require<KangPhaseEndOrderOperation>();
        if (operation.Deferred || input.IsDecline || input.Targets.Count != 0 || input.Spent.Count != 0
            || input.DefinedValues.Count != 0 || input.Allocated.Count != 0
            || input.Affordance is not (KangFirst or OthersFirst))
            throw new ArgumentException("choose which of the due phase-end effects resolves first");
        if (input.Affordance == OthersFirst)
        {
            ResolveOthers(world, operation.Owner, events);
            BeginKang(world, operation.Owner, events);
            return;
        }
        BeginKang(world, operation.Owner, events);
        world.Agenda.ContinueBeforeOwner(operation.Origin, operation.Owner,
            step with { Operation = operation with { Deferred = true } });
    }

    internal static void BeginKang(World world, PhaseStep owner, List<GameEvent> events)
    {
        world.Agenda.KangTransitions.Begin(world, owner, events);
        world.Agenda.KangSplit.Begin(world, owner, events);
    }

    private static void ResolveOthers(World world, PhaseStep owner, List<GameEvent> events)
    {
        // rr:delayed-effect.1: each resolves "automatically and immediately".
        foreach (var condition in StepConditions.ConditionsOf(owner.What))
            DelayedEffects.Occur(world, condition, events);
    }

    private static Prompt Ask(World world) =>
        new(world.FirstPlayer, Question.Option, TimingPriority.Continuous,
            KangPhaseEndOrderOperation.Order, "Choose which phase-end effect resolves first", false,
            [
                new Affordance(KangFirst, "Resolve_Kang_First", -1, world.FirstPlayer, "Kang")
                {
                    DisplayLabel = "Resolve Kang's effect first",
                    CommitLabel = "Resolve Kang first",
                    Description = "Resolve Kang's delayed effect completely, then the other delayed effects due now.",
                },
                new Affordance(OthersFirst, "Resolve_Delayed_First", -1, world.FirstPlayer, "delayed effects")
                {
                    DisplayLabel = "Resolve the other delayed effects first",
                    CommitLabel = "Resolve the others first",
                    Description = "Resolve the other delayed effects due now, then Kang's delayed effect.",
                },
            ])
        {
            DisplayQuestion = "Choose which phase-end effect resolves first",
            Description = "Kang's delayed effect and other delayed effects are due at the same phase ending. The first player chooses their order.",
        };
}
