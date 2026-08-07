from . import *

# Down Time

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players",
        ),
        *AbilityFactory.GiveKeywordToAttached(
            AlterEgo,
            recover=2,
        ),
    ]

