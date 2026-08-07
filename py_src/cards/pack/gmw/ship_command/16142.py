from . import *

# * Milano

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.FirstPlayerControlThis(),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("G")
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .AnyPlayerCanDoThis(),
    ]

