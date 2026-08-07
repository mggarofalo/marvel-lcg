from . import *

# Aerial Bombardment

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedVillain",
            CardFinder(non_trait="AERIAL"),
            ignore_retaliate=True,
        ),
        *AbilityFactory.UnitGetATKWhileAttacking(
            AbilityType.NonKeyword,
            "AttachedCharacter",
            CardFinder(non_trait="AERIAL"),
            1,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("BB"))
        .SetCostFunc(CostFunc.Exhaust("YourHero")),
    ]

