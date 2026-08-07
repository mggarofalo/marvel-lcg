from . import *

# Accelerated Reflex

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Quicksilver"),
            defense=1,
        )
    ]

