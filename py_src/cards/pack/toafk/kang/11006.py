from . import *

# * Kang (The Conqueror) (III)

def GetAbilities() -> Sequence['Ability']:

    return [
        KangTheConquerorAttack(),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            lambda effect, message:
                Worlds.SetGameOver(True, effect)
        )
    ]

