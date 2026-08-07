from . import *

# Attack Training

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder2("X-MEN", Ally)
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Ally,
            attack=1,
            health=2,
        ),
    ]

