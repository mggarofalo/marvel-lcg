from . import *

# Black Widow's Gauntlet

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("G"),
            for_card=CardFinder2("PREPARATION")
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

