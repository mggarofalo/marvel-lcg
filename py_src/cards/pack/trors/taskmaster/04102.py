from . import *

# * Taskmaster's Sword

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Taskmaster")
        ),
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Taskmaster"),
            piercing=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("BR"))
        .SetCostFunc(CostFunc.Exhaust("YourHero"))
    ]

