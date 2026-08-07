from . import *

# Molecular Control

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Apocalypse")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "Character",
            retaliate=1,
            stalwart=1,
        ),
        AbilityFactory.WhenThisBoostAttachTo(
            CardFinder(name="Apocalypse")
        ),
    ]

