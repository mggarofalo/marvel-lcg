from . import *

# Basic Defense Upgrade

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=3,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            defense=1,
        ),
    ]

