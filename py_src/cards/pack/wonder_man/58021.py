from . import *

# Heroic Conditioning

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players",
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=3,
            thwart=1
        ),
    ]

