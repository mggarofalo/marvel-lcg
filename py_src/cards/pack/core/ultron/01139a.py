from . import *

# Countdown to Oblivion - 3A

def GetAbilities() -> Sequence['Ability']:

    def countdown_to_oblivion(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            CreateUltronFacedownDrone(player, 1, effect)

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            countdown_to_oblivion
        ),
    ]
