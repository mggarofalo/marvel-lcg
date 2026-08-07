from . import *

# Love Triangle

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourAlly",
            if_cannot_gain_surge=True
        ),
        *AbilityFactory.UnitCannotAttackTarget(
            "AttachedAlly",
            cannot_attack=Villain
        ),
        *AbilityFactory.UnitCannotDefend(
            "AttachedAlly",
            Villain,
            cannot_trigger_defense_ability=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCost(Cost("B"))
        .SetCostFunc(CostFunc.Exhaust("Attached")),
    ]

