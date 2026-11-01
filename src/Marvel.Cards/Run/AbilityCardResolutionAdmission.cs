using static Marvel.Cards.Run.AbilityCardStateAdmission;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
namespace Marvel.Cards.Run;

/// <summary>Assesses completion of card movement and readiness instructions.</summary>
internal static class AbilityCardResolutionAdmission
{
    internal static AdmissionResolution? ResolutionOf(
        AbilityEffect node, AbilityAdmissionScope cast) => node.OperationName() switch
        {
            "moveActiveCounter" => AbilityActiveCounterExecution.CanMove(node, cast)
                ? AdmissionResolution.Full : AdmissionResolution.None,
            "putIntoPlay" when node is AbilityEffect.PutIntoPlay { AllyUnderYourControl: true } entry =>
                AbilityAllyEntry.HasTarget(entry, cast) ? AdmissionResolution.Full : AdmissionResolution.None,
            "placeOnBottomOfDeck" or "placeOnTopOfDeck" => AbilityDeckBottomMovement.Resolution(
                ((AbilityEffect.CardAction)node).Selection, cast),
            "exhaust" => ResolutionOfCards(
                Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast), card => card.Ready),
            "ready" => ResolutionOfCards(
                Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast), card => !card.Ready
                    && AbilityProgramQueries.CanReady(cast.World, cast.Context.Program, card)),
            "declareDefender" => Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } declared
                && Attack.CanDeclareByAbility(
                    cast.World, cast.World.Facts, declared,
                    ReplaceableDefenseDefender(cast))
                    ? AdmissionResolution.Full
                    : AdmissionResolution.None,
            "discard" => EffectOf<AbilityEffect.CardAction>(node, cast).Selection is var discardTarget
                && Find(discardTarget, cast) is { } discarded
                && CanRemoveByEffect(discardTarget, cast, discarded)
                    ? AdmissionResolution.Full
                    : AdmissionResolution.None,
            _ => null,
        };

}
