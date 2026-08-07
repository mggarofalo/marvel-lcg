from . import *

# Kang's Arrival - 1B

def GetAbilities() -> Sequence['Ability']:

    def kangs_arrival(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            player.DealEncounterCards(1, effect)

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            kangs_arrival
        ),
    ]

