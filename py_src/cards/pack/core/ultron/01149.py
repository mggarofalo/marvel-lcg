from . import *

# Invasive AI

def GetAbilities() -> Sequence['Ability']:

    def invasive_ai(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            player.DiscardDeckTopCards(3, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            invasive_ai
        )
    ]
