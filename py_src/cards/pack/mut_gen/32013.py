from . import *

# Protective Training

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder2("X-MEN", Ally)
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Ally,
            health=3,
        )
    ]

