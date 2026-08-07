from . import *

# Atlantean Guard

def GetAbilities() -> Sequence['Ability']:

    def atlantean_guard_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardDeckTopCards(3, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            atlantean_guard_revealed
        ),
    ]

