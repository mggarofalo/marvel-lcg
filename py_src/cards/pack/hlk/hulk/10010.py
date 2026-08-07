from . import *

# Immovable Object

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=4,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Hulk"),
            retaliate=1,
        ),
    ]

