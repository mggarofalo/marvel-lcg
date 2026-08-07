from . import *

# Assault on NORAD - 2A

def GetAbilities() -> Sequence['Ability']:

    def assault_on_norad(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            CreateUltronFacedownDrone(player, 1, effect)

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            assault_on_norad
        )
    ]
