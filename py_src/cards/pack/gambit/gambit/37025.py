from . import *

# Guild Business

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.PlayerActionToRemoveThisFromGame(
            AbilityType.AlterEgoAction,
        ).SetCost(Cost("Y"))
        .SetCostFunc(CostFunc.Exhaust("YouControlUnit", name="Remy LeBeau")),
    ]

