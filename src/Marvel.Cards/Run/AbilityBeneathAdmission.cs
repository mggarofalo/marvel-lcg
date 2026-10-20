using static Marvel.Cards.Run.AbilityAdmission;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityCardActionTargetAdmission;

namespace Marvel.Cards.Run;

internal static class AbilityBeneathAdmission
{
    internal static TargetLegality Target(AbilityEffect.ReleaseBeneath release, AbilityAdmissionScope context) =>
        release.RemoveHost || BeneathCards.Of(context.World, context.Source).Count > 0
            ? TargetLegality.Valid : TargetLegality.None;

    internal static TargetLegality Target(AbilityEffect.StoreBeneath store, AbilityAdmissionScope context)
    {
        var host = Find(store.Host, context);
        if (host is null || !DeckTypes.IsInPlay(host.Area.Type)) return TargetLegality.Invalid;
        return CardsLegality(Every(store.Cards, context).Where(card => card != host
            && !(BeneathCards.IsStored(card.Area.Type) && card.Area.Host == host.ObjectId)));
    }
}
