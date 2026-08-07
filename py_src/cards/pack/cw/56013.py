from . import *

# Aggressive Conditioning

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard("Players"),
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=3
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            attack=1
        ),
    ]

