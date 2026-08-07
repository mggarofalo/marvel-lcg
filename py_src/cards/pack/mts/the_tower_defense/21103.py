from . import *

# * Proxima's Spear

def GetAbilities() -> Sequence['Ability']:
    
    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Proxima Midnight")
        ),
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Proxima Midnight"),
            overkill=True,
            piercing=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YB"))
        .SetCostFunc(CostFunc.DealDamage(1, "Initiator")),
    ]

