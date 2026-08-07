from . import *

# Hyper Perception

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Quicksilver"),
            thwart=1,
        )
    ]

