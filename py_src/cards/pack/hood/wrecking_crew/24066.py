from . import *

# * Bulldozer

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            overkill=True
        ),
    ]

