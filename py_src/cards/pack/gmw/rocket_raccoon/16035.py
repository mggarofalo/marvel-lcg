from . import *

# Battery Pack

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=3,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            attack=1,
        ),
    ]

