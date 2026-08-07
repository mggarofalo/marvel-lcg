from . import *

# * Death

def GetAbilities() -> Sequence['Ability']:

    def death(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetAgainstPlayer()

        if player and this.health >= 1:
            this.DealDamage(player.GetControlCharacters(), 1, effect)


    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            death
        ),
        *ThisCannotBeDefeatedWhileAnotherVillainHas1HP()
    ]

