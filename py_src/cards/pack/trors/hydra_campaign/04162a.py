from . import *

# Basic Recovery Upgrade

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=4,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            AlterEgo,
            recover=1,
        ),
    ]

