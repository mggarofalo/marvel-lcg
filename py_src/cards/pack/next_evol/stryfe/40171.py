from . import *

# Mind Trap

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        AbilityFactory.CardsEnterPlayExhausted(
            CardFinder(card_type=Ally|Upgrade|Support),
            "AttachedPlayer",
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Exhaust(range=(3, 3), from_where=["YouControlCards"])),
    ]

