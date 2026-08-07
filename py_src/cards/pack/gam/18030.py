from . import *

# Comms Implant

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder2("GUARDIAN", Ally)
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Ally,
            thwart=1,
            health=1,
        ),
    ]

