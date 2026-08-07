from . import *

# Super-Soldier Serum

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("R")
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

