from . import *

# * Karmic Staff

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("G"),
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

