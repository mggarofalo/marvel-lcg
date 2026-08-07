from . import *

# Genetic Enhancement

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Dark Beast")
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Exhaust("YourHero"))
        .SetCostFunc(CostFunc.ResolveAbility(
            card_type=Environment,
            trait="SETTING")),
        AbilityFactory.WhenThisBoostAttachTo(
            CardFinder(name="Dark Beast")
        ),
    ]

