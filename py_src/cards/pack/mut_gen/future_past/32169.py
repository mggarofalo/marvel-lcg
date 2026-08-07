from . import *

# Bastion's Machinations

def GetAbilities() -> Sequence['Ability']:

    def bastions_machinations_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            faces = player.player_deck.GetTops(9)
            this.PlaceCardHere(faces, False, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            bastions_machinations_revealed
        ),
    ]

