from . import *

# Wrapped in Metal

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        *AbilityFactory.UnitCannotThwartTarget(
            "AttachedIdentity",
            cannot_thwart=True,
            cannot_trigger_thwart_ability=True
        ),
        *AbilityFactory.UnitCannotAttackTarget(
            "AttachedIdentity",
            cannot_attack=True,
            cannot_trigger_attack_ability=True
        ),
        *AbilityFactory.UnitCannotDefend(
            "AttachedIdentity",
            None,
            cannot_trigger_defense_ability=True
        ),
        *AbilityFactory.UnitCannotRecover(
            "AttachedIdentity",
            cannot_trigger_recover_ability=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.Action,
        ).SetCost(Cost("R"))
        .SetCostFunc(CostFunc.Exhaust("YourIdentity")),
    ]

