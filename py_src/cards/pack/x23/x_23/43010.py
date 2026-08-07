from . import *

# Grim Resolve

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("G")
        ).SetCostFunc(CostFunc.TakeDamage(1, "Initiator"))
        .SetCostFunc(CostFunc.Exhaust("This")),
    ]

