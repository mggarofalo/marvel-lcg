from . import *

# Keen Instincts

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("G"),
            for_card=CardFinder(traits=["ATTACK", "THWART"], card_type=Event)
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

