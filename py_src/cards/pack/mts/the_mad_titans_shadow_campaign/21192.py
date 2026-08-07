from . import *

# * Hogun

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True
        ),
    ]

