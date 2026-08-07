from . import *

# Hunted

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            DiscardThisCard
        ).SetCostFunc(CostFunc.Discard("YourHandCards"))
    ]

