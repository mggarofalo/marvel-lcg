from . import *

# Danger Room Training

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder2("X-MEN", Ally)
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Ally,
            thwart=1,
            attack=1,
            health=1,
        ),
    ]

