from . import *

# * The X-Jet

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("G"),
            for_player_whose_identity_has_trait="X-MEN"
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

