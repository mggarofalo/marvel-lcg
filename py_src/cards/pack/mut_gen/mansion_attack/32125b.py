from . import *

# The Brotherhood Strikes! B

def GetAbilities() -> Sequence['Ability']:

    def the_brotherhood_strikes_b_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            player.DealEncounterCards(1, effect)
        Players.ForEachPlayer(effect, action)

        this.Advance(None, effect)
        Faces.AddToVictoryDisplay([this], effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_brotherhood_strikes_b_revealed
        ),
    ]

