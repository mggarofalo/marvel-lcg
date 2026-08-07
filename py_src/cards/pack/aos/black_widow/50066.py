from . import *

# * Black Widow

def GetAbilities() -> Sequence['Ability']:

    def black_widow_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        this.PlaceThreatOnSchemes("MainScheme", "3*", effect)


    def black_widow(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.attacker.GetControlByPlayer()
        face = Worlds.DiscardEncounterTopCard(effect)
        if face:
            face.ResolvePreparationAbility(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            black_widow_revealed
        ),
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.ForcedInterrupt,
            "YouControlCharacter",
            "This",
            black_widow,
        ).SetCostFunc(CostFunc.RemoveThreatFrom("MainScheme", 1))
    ]

