from . import *

# * Gauntlet

def GetAbilities() -> Sequence['Ability']:

    def gauntlet(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()

        if player:
            player.DiscardControlCards(effect, upgrade=True)


    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            gauntlet
        ),
    ]

