from . import *

# Focusing Crystal

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            MODOK_FINDER
        ),
        AfterMODOKHitPointsAreResetDiscardThisCard()
    ]

