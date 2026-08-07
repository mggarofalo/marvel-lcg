from . import *

# * Mark V Armor

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=6,
        ),
    ]

