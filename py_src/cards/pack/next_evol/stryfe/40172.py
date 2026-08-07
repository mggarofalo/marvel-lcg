from . import *

# Psionic Amnesia

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        AbilityFactory.IncreaseResourceCostToPlay(
            CardFinder(card_type=Ally|Support),
            +2,
            "You",
        ),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "AttachedPlayer",
            CardFinder(card_type=Ally|Support),
            DiscardThisCard
        ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
    ]

