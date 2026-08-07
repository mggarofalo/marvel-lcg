from . import *

# Martial Prowess

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players"
        ),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("R"),
            CardFinder2("ATTACK", Event),
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

