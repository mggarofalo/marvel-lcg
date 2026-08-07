from . import *

# Vulture

def GetAbilities() -> Sequence['Ability']:

    def vulture(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardRandomHandCards(1, effect)


    return [
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            vulture
        ),
    ]

