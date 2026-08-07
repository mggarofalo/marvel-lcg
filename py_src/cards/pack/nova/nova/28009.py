from . import *

# * Supernova Helmet

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            trait="AERIAL",
        ),
        AbilityFactory.CanGenerateResources(
            AbilityType.HeroResource,
            Resources("G"),
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

