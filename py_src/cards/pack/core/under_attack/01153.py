from . import *

# Concussion Blasters

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Villain,
            retaliate=1,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YY"))
        .SetCostFunc(CostFunc.Exhaust("YourHero"))
    ]

