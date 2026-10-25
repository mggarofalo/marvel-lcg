using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;

namespace Marvel.Cards.Run;

/// <summary>Validates a public physical villain/deck relation without inspecting hidden cards.</summary>
internal static class AbilitySelectedDeckReveal
{
    internal static bool HasTarget(AbilityEffect effect, AbilityAdmissionScope scope)
    {
        var card = Find(((AbilityEffect.CardAction)effect).Selection, scope);
        if (card is null) return false;
        _ = EncounterRouting.CorrespondingGroup(scope.World, card);
        return true;
    }
}
