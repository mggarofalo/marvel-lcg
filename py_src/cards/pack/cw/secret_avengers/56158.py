from . import *

# Switching Sides

def GetAbilities() -> Sequence['Ability']:

    def switching_sides_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        player.AskDiscardFaces(player.GetControlAllies(), (1, 1), effect)
        if Worlds.IsCompetitiveMode(effect):
            assert False


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            switching_sides_revealed
        ),
    ]

