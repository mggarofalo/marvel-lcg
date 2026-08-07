from . import *

# Armored Vest

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players"
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            defense=1,
        )
    ]

