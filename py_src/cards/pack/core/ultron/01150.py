from . import *

# Ultron's Imperative

def GetAbilities() -> Sequence['Ability']:

    def ultrons_imperative(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:

        first_player = Worlds.GetFirstPlayer(effect)

        CreateUltronFacedownDrone(first_player, 2, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            ultrons_imperative
        )
    ]
