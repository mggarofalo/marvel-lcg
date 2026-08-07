from . import *

# * Barrage

def GetAbilities() -> Sequence['Ability']:

    def barrage(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            this.DealDamage(player.GetControlCharacters(), 1, effect)


    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            barrage
        ),
    ]

