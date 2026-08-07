from . import *

# Ingenuity

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
        ).SetPlay(only_if_your_identity_has_trait="GENIUS"),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("B")
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

