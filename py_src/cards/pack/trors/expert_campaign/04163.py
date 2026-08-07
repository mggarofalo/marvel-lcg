from . import *

# Zola's Algorithm

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCost(Cost("B"))
        .SetCostFunc(CostFunc.Exhaust("YourAlterEgo")),
    ]

