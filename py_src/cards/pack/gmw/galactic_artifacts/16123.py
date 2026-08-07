from . import *

# * Obedience Potion

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "Character",
            thwart=-1,
            attack=-1,
            defense=-1,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("BB"))
        .SetCostFunc(CostFunc.TakeDamage(1, "YourIdentity"))
        .AnyPlayerCanDoThis(),
    ]

