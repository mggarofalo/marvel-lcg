from . import *

# * Baron Zemo

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.UnitCannotThwartTarget(
            "EngagedIdentity",
            cannot_thwart=True,
        )
    ]

