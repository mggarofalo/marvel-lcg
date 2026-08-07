from . import *

# Expert Marksman

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("G"),
            CardFinder2("ARROW", Event),
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

