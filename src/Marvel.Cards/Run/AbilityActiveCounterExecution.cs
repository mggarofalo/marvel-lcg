using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

/// <summary>Resolves a single printed active-counter destination.</summary>
internal static class AbilityActiveCounterExecution
{
    internal static void Move(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        if (AbilityCardStateExecution.Find(selection, context) is { } destination)
            EncounterActiveCounter.Move(context.World, destination, context.Trigger, context.Events);
    }

    internal static bool CanMove(AbilityEffect node, AbilityAdmissionScope cast) =>
        AbilityInitiationPrimitives.Find(((AbilityEffect.CardAction)node).Selection, cast) is { } destination
        && EncounterActiveCounter.CanMove(cast.World, destination);
}
