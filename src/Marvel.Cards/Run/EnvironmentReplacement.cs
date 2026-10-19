using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Discards the existing environment set while retaining the triggering entrant.</summary>
internal static class EnvironmentReplacement
{
    internal static bool DiscardOthers(AbilityImmediateContext context)
    {
        var query = context.Admission.Query;
        var world = query.World;
        int entrant = query.Occurrence.Subject;
        if (entrant < 0 || entrant >= world.Cards.Count
            || EffectiveCards.Kind(world.Cards[entrant], world.Facts) != CardKind.Environment)
            throw new RulesNotImplementedException("environment replacement requires a triggering environment");
        var prior = context.Admission.Selectors().Every(
            new AbilityCardSelection.Query(AbilityCardQuery.OtherEnvironments)).ToArray();
        foreach (var card in prior)
            if (Discard.EffectCanRemove(world, world.Facts, context.Admission.Source, card))
                Discard.CardFromEffect(world, world.Facts, context.Admission.Source, card, context.Trigger, context.Events);
        return prior.Any(card => !DeckTypes.IsInPlay(card.Area.Type));
    }
}
