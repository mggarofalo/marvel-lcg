from . import *

# God of Thunder

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.HeroResource,
            Resources("Y")
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

