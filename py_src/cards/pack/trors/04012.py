from . import *

# * Black Knight

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            is_basic_attack=True,
            piercing=True
        )
    ]

