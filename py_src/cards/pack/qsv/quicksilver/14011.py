from . import *

# Reinforced Sinew

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Quicksilver"),
            attack=1,
        )
    ]

