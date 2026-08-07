from . import *

# * Drax's Knife

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            attack=1,
        )
    ]

