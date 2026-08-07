from . import *

# * Wolfsbane: Rahne Sinclair

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True
        ),
    ]

