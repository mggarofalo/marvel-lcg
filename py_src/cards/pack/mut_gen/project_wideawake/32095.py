from . import *

# Learning A.I.

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Villain,
            retaliate=1,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("BBB")),
        AbilityFactory.WhenThisBoostAttachTo(
            "Villain",
        ),
    ]

