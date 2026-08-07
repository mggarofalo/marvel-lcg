from . import *

# * Crossfire's Rifle

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Crossfire"),
            if_cannot_attach_to=Villain
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedEnemy",
            ranged=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("G"))
        .SetCostFunc(CostFunc.Exhaust("YourHero"))
    ]

