from . import *

# Sinister Ends

def GetAbilities() -> Sequence['Ability']:

    def sinister_ends_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            player.DealEncounterCards(1, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sinister_ends_revealed
        ),
    ]

