using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityRepeatedStatusTrace;
using static Marvel.Cards.Run.AbilityTargetAdmission;

namespace Marvel.Cards.Run;

/// <summary>Public target availability for leaf instructions before their rule-specific resolution.</summary>
internal static class AbilityLeafTargetAvailability
{
    /// <summary>Target existence for leaf options without a dedicated partial-resolution check.</summary>
    internal static bool HasTargets(AbilityEffect node, AbilityAdmissionScope cast) => node.OperationName() switch
    {
        "revealFromVillainDeck" => AbilitySelectedDeckReveal.HasTarget(node, cast),
        "reveal" or "returnToHand" =>
            Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast).Count > 0,
        "soakDamage" or "attachTo" or "discard" =>
            Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is not null,
        "giveStatus" => StatusTargets(node, cast).Count > 0,
        "declareDefender" => Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } declared
            && Attack.CanDeclareByAbility(
                cast.World, cast.World.Facts, declared,
                ReplaceableDefenseDefender(cast)),
        "grantUntil" => Find(GrantSelectionOf(node, cast), cast) is not null,
        "dealEncounterCard" => Find(EffectOf<AbilityEffect.DealEncounterCard>(node, cast).Card, cast) is not null,
        "indirectDamage" => Amount(EffectOf<AbilityEffect.IndirectDamage>(node, cast).Amount, cast) <= 0
            || Assignable(DamageSelectionOf(node, cast), cast).Count > 0,
        "dealDamage" or "dealAttackDamage" => DamageTargets(DamageSelectionOf(node, cast), cast).Count > 0,
        "placeThreat" => Every(ThreatSelectionOf(node, cast), cast).Count > 0,
        "placeAccelerationToken" => cast.World.TheCardIn(DeckType.MainSchemesArea) is not null,
        "enemyAttacks" or "enemySchemes" => Every(ActivationOf(node, cast).Enemies, cast).Count > 0,
        "putIntoPlay" => AbilityAllyEntry.HasTarget(EffectOf<AbilityEffect.PutIntoPlay>(node, cast), cast),
        "placeAtRandom" => Find(EffectOf<AbilityEffect.PlaceAtRandom>(node, cast).Host, cast) is not null,
        "search" => HasSearchableArea(node, cast),
        // Admission uses only the public deck count, never the hidden matching cards.
        "discardUntil" => node is not AbilityEffect.DiscardUntil { From: AbilitySearchArea.YourDeck } discard
            || cast.World.Seats[Seat(discard.Player, cast)].Deck.Cards.Count > 0,

        // The delayed effect's game element comes from its future occurrence;
        // rr:target.5 requires no target at initiation.
        "delayUntil" => true,
        "generate" or "preventDamage" or "cancelWhenRevealed" or "cancelOccurrence"
            or "dealEncounterCards" or "revealTop"
            or "recoverDiscardedByResource" or "shuffleInto" or "recruitDiscarded" or "shuffle" => true,
        _ => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' uses '{node.OperationName()}' in an option whose target "
            + "legality is not implemented"),
    };

}
