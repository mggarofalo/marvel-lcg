using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilityDelayedExecution
{
    internal static void CompleteActivation(
        AbilityResolutionExecution execution, World world, EnemyActivation result,
        List<GameEvent> events)
    {
        if (world.AbilityRuntime is null) return;
        foreach (var effect in AbilityGameRuntime.For(world, execution.program).CompleteActivation(result.Id))
            Run(execution, world, result, effect, events);
    }

    private static void Run(
        AbilityResolutionExecution execution, World world, EnemyActivation result,
        ActivationEffect effect, List<GameEvent> events)
    {
        var capture = effect.Capture;
        var source = world.Cards[capture.Invocation.Source.ObjectId];
        var ability = execution.AbilityAt(source, capture.Invocation.Address.Tier,
            capture.Invocation.Address.Ordinal, capture.Invocation.Address.Face);
        if (AbilityContinuationFrames.NodeAtTypedPath(ability.Effect, capture.Cursor.Frames)
            is not AbilityEffect.AfterActivation delayed)
            throw new InvalidOperationException("the delayed frame does not identify an after-activation effect");

        var cast = new AbilityResolutionState(world, source, capture.Invocation.Occurrence, capture.Cursor.Player, events)
        {
            Tier = capture.Invocation.Address.Tier,
            AbilityPlayer = capture.Invocation.AbilityPlayer,
            EventTrigger = capture.Invocation.Trigger,
            FinalStep = capture.Cursor.FinalStep,
        };
        execution.RestorePersisted(cast, capture);
        cast.RestoreAbility(capture.Invocation.Address.Ordinal,
            capture.Cursor.Frames.Add(new AfterActivationFrame()), capture.Invocation.Address.Face);
        AbilityContinuations.RecordImmediateActivationResult(cast.Results, result);
        cast.Results["activationDamageToYou"] = result.DamageRecipients.Contains(
            world.Seats[capture.Cursor.Player].IdentityCard.ObjectId) ? 1 : 0;
        if (effect.Altered >= 0) cast.BindAlteration(world.Cards[effect.Altered]);
        execution.Run(delayed.Effect, cast);
    }
}
