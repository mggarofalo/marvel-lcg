from . import *

# * Taskmaster's Shield

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Taskmaster")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Taskmaster"),
            retaliate=1,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("BR"))
        .SetCostFunc(CostFunc.Exhaust("YourHero"))
    ]

