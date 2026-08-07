from . import *

# * Mysterio

def GetAbilities() -> Sequence['Ability']:

    def mysterio_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def action(player: 'Player'):
            player.DiscardDeckTopCards(5, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            mysterio_revealed
        ),
        Mysterio(3)
    ]

