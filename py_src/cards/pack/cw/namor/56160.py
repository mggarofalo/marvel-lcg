from . import *

# * Namor

def GetAbilities() -> Sequence['Ability']:

    def namor_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        this.DoActivate(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            namor_revealed
        ),
    ]

