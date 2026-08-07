from . import *

# * The Magus

def GetAbilities() -> Sequence['Ability']:

    def the_magus_revealed(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        player.DiscardDeckTopCards(5, effect)


    return [
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            the_magus_revealed,
        ),
    ]

