from . import *

# * Black Dwarf

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            overkill=True
        ),
    ]

