from . import *

# Endurance

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players",
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=3,
        ),
    ]

