from . import *

# Clarity of Purpose

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(Friend),
        AbilityFactory.CanGenerateResources(
            AbilityType.HeroResource,
            Resources("G")
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.DealDamage(1, "Attached")),
    ]

