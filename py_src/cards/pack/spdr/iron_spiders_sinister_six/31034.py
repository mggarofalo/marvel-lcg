from . import *

# * Iron Spider

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            overkill=True
        ),
    ]

