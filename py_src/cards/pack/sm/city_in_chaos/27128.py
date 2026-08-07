from . import *

# * Rhino

def GetAbilities() -> Sequence['Ability']:

    return [

        AbilityFactory.UnitAttackGainKeyword(
            "This",
            overkill=True,
            piercing=True
        ),
    ]

