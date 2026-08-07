from . import *

# Under Surveillance

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(MainScheme),
        *AbilityFactory.GiveKeywordToAttached(
            Scheme2,
            target_threat=4,
        )
    ]

