from . import *

# Martial Law

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "You",
            hand_size=-1,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCost(Cost("Y"))
        .SetCostFunc(CostFunc.DealPlayerEncounterCard(1, "Initiator")),
    ]

