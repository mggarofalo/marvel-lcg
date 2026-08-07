from . import *

# Basic Attack Upgrade

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=1,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            attack=1,
        ),
    ]

