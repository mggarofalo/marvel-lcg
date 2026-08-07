from . import *

# Flight

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Villain,
            trait="AERIAL",
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedVillain",
            overkill=True
        ),
    ]

