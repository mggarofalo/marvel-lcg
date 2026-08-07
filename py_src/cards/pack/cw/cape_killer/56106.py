from . import *

# Unregistered Super

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "You",
            trait="UNREGISTERED",
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Discard("YourHandCards", card_class="IdentitySpecific")),
    ]

