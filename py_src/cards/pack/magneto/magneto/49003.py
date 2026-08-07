from . import *

# * Magneto's Helmet

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Magneto"),
            steady=1,
        ),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("G"),
            for_card=CardFinder2("MAGNETIC")
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

