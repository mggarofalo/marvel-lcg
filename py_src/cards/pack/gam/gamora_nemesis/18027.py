from . import *

# In A Bind

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Gamora")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Gamora"),
            treat_as_blank=True,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Discard("YourHandCards", trait="ATTACK", card_type=Event))
        .SetCostFunc(CostFunc.DealDamage(1, name="Gamora")),
    ]

