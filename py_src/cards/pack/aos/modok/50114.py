from . import *

# Automated Mobile Unit

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            MODOK_FINDER
        ),
        *AbilityFactory.GiveKeywordToAttached(
            MODOK_FINDER,
            health=5,
        ),
        AfterMODOKHitPointsAreResetDiscardThisCard()
    ]

