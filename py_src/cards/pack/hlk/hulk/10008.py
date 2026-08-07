from . import *

# * Banner's Laboratory

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Bruce Banner"),
            recover=2,
        ),
        AbilityFactory.CanGenerateResources(
            AbilityType.AlterEgoResource,
            Resources("B")
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

