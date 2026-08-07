from . import *

# Nerves of Steel

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players"
        ),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("Y"),
            CardFinder2("DEFENSE", Event),
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

