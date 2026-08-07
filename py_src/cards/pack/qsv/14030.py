from . import *

# Sense of Justice

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players",
        ),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("B"),
            CardFinder2("THWART", Event),
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

