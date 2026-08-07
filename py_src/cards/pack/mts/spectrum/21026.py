from . import *

# Loss of Control

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.PlayerActionToRemoveThisFromGame(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
        AbilityFactory.PlayersCannotChangeAdditionalForms(
            "You"
        )
    ]

