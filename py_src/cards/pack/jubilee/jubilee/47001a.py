from . import *

# * Jubilee

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("G")
        ).SetName('"Like, totally!"')
        .SetCostFunc(CostFunc.Exhaust("This")),
    ]

