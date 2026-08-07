from . import *

# * Scrambler

def GetAbilities() -> Sequence['Ability']:

    def scrambler_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, upgrade=True)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            scrambler_revealed
        ),
    ]

