from . import *

# Gobbler Glider

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="The Green Gobbler"),
            if_cannot_attach_to=Minion
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            trait="AERIAL",
        )
    ]

