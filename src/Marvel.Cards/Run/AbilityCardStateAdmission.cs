using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityDelayedReachability;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

using static Marvel.Cards.Run.AbilityAdmissionResolutionPreflight;
namespace Marvel.Cards.Run;

internal static class AbilityCardStateAdmission
{
    internal static bool? CardStatePartialResolution(
        AbilityEffect node, AbilityAdmissionScope cast) => node.OperationName() switch
        {
            "removeFromGame" => Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } card
                && CanRemoveByEffect(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast, card),
            "exhaust" => Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast)?.Ready == true,
            "returnOwnedToDiscard" => Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast)
                .Any(card => card.Owner >= 0),
            "returnOwnedToHand" or "addToHand" => AbilityCardMovementAdmission.HasHandTarget(node, cast),
            "ready" => Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast).Any(card =>
                !card.Ready && AbilityProgramQueries.CanReady(cast.World, cast.Context.Program, card)),
            "removeCounters" => CounterRemovalOf(node, cast) is var removal
                && Find(removal.Card, cast) is { } counterCard
                && CounterKeyForRemoval(
                    counterCard, removal.Counter, removal.Count) is not null,
            "advanceMainScheme" => CanAdvanceMainScheme(cast),
            "discardAtRandom" => Amount(EffectOf<AbilityEffect.DiscardAtRandom>(node, cast).Count, cast) > 0
                && Seats(EffectOf<AbilityEffect.DiscardAtRandom>(node, cast).Players, cast)
                    .Any(seat => cast.World.Seats[seat].Hand.Cards.Count > 0),
            "discardTop" => Amount(EffectOf<AbilityEffect.DiscardTop>(node, cast).Count, cast) > 0
                && DiscardTopHasCards((AbilityEffect.DiscardTop)node, cast),
            "heal" => Find(EffectOf<AbilityEffect.Heal>(node, cast).Card, cast) is { Damage: > 0 }
                && Amount(EffectOf<AbilityEffect.Heal>(node, cast).Amount, cast) > 0,
            _ => null,
        };

}
