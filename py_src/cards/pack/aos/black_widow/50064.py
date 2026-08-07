from . import *

# * Black Widow

def GetAbilities() -> Sequence['Ability']:

    def black_widow(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.attacker.GetControlByPlayer()
        face = Worlds.DiscardEncounterTopCard(effect)
        if face:
            face.ResolvePreparationAbility(player, effect)

    return [
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.ForcedInterrupt,
            "YouControlCharacter",
            "This",
            black_widow,
        ).SetCostFunc(CostFunc.RemoveThreatFrom("MainScheme", 1, ignore_crisis=True))
    ]

