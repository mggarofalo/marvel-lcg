from . import *

# * Corvus Glaive

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Corvus Glaive")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Corvus Glaive"),
            retaliate=1,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YR"))
        .SetCostFunc(CostFunc.DealDamage(1, "Initiator")),
    ]

