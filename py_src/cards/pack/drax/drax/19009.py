from . import *

# * Drax's Other Knife

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(card_type=Hero, name="Drax"),
            retaliate=1,
        )
    ]

