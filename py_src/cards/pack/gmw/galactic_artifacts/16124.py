from . import *

# * The Beyonder's Blazer

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Enemy,
            highest_sch=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("2", same_type=True))
        .SetCostFunc(CostFunc.PlaceThreatOn(2, card_type=MainScheme)),
    ]

