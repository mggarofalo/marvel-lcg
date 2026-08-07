from . import *

# * Beetle Armor MK IV

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Beetle"),
            if_cannot_attach_to=Villain,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "Character",
            health=4,
        ),
    ]

