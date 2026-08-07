from . import *

# Tracking Display

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        *AbilityFactory.UnitCannotDefend(
            Unit2,
            "AttachedVillain",
            cannot_trigger_defense_ability=True,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Exhaust("YouControlUnit"))
        .SetCostFunc(CostFunc.Discard("RandomHandCard")),
    ]

