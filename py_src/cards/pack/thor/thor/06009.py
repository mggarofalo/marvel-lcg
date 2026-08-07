from . import *

# * Mjolnir

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Thor"),
            attack=1,
            trait="AERIAL",
        )
    ]

