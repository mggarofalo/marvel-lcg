from . import *

# Heroic Intuition

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players"
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            thwart=1,
        )
    ]

