from . import *

# The Crimson Cowl - 1B

def GetAbilities() -> Sequence['Ability']:

    def the_crimson_cowl(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            CreateUltronFacedownDrone(player, 1, effect)

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_crimson_cowl
        ),
    ]
