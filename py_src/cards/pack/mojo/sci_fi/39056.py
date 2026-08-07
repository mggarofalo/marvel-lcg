from . import *

# Magneto 2.6

def GetAbilities() -> Sequence['Ability']:

    def magneto_26_revealed(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()

        Faces.PlaceCountersOn([this], 1, 'mangetic', effect)

        num = this.GetCounters('mangetic')

        player.DiscardHandCards(("Zero", num), effect)


    return [
        AbilityFactory.AfterEnemyActivationEnd(
            AbilityType.ForcedResponse,
            "This",
            magneto_26_revealed
        ),
    ]

