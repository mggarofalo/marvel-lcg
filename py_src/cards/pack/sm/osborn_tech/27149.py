from . import *

# Kinetic Armor

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
        ).SetCostFunc(CostFunc.TakeIndirectDamage(3)),
    ]

