from . import *

# Arm Cannon

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedVillain",
            overkill=True,
            piercing=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Discard(
            card_type=Upgrade,
            highest_cost=True,
            from_where=["YouControlCards"])),
    ]

