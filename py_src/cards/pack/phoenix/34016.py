from . import *

# Mission Training

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder2("X-MEN", Ally)
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Ally,
            thwart=1,
            health=2,
        ),
    ]

