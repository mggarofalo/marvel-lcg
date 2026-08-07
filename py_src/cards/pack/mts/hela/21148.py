from . import *

# * Hela's Cloak

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Hela")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Hela"),
            stalwart=1,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            AttachThisToHela
        ),
    ]

