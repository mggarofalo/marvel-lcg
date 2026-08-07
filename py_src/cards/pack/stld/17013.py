from . import *

# * Yondu

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            ranged=True,
        )
    ]

