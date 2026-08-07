from . import *

# Adaptive Armor

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Villain,
            health=8,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YYY")),
        AbilityFactory.WhenThisBoostAttachTo(
            "Villain",
        ),
    ]

