from . import *

# * Jemma Simmons

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ReduceCostToPlayThis(
            lambda effect: 2,
            your_identity_has_traits=["S.H.I.E.L.D"]
        ),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("B"),
            CardFinder2("TECH")
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

