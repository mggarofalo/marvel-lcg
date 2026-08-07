from . import *

# Basic Thwart Upgrade

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=2,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            thwart=1,
        ),
    ]

