from . import *

# * Fenris Wolf

def GetAbilities() -> Sequence['Ability']:

    def fenris_wolf(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        # player = message.GetToPlayer()
        # identity = player.GetIdentity()


    return [
        WhenDefeatedPlaceShatterCountersOnTheAvatarOfLokivillain(3, fenris_wolf)
    ]

