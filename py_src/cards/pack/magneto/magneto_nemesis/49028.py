from . import *

# * Exodus

def GetAbilities() -> Sequence['Ability']:

    def exodus(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()

        if player:
            value = this.attack
            player.DiscardDeckTopCards(value, effect)


    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            exodus
        ),
    ]

