from . import *

# Balance the Scales

def GetAbilities() -> Sequence['Ability']:

    def balance_the_scales_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            player.player_deck.ShuffleWithDiscardPile(False, effect)
            faces = player.player_deck.GetAll(True)[:player.player_deck.GetSize()//2]
            Faces.RemoveAllFromGame(faces, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            balance_the_scales_revealed
        ),
    ]

