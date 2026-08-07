from . import *

# * Winter Armor

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=3,
            steady=1,
        ),
    ]

