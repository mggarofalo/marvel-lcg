from . import *

# Lethal Weapon

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Gamora"),
            if_cannot_attach_to=Villain
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Discard(
            card_type=Upgrade,
            from_where=["YouControlCards"]
        )),
    ]

