from . import *

# Physical Strain

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Magneto")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Magneto"),
            steady=-1,
        ),
    ]

