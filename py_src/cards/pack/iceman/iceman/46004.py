from . import *

# Power Belt

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=3,
        ),
        AbilityFactory.CanGenerateResources(
            AbilityType.HeroResource,
            Resources("G"),
            for_card=CardFinder2("ICE")
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

