from . import *

# Side Holster

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players",
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            control_additional_restricted=CardFinder2("WEAPON", Upgrade),
        )
    ]

