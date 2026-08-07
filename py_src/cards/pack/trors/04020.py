from . import *

# War Machine

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            is_basic_attack=True,
            ranged=True
        )
    ]

