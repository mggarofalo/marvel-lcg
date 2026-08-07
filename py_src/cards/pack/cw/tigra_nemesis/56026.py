from . import *

# * The Hood's Mantle

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="The Hood"),
            otherwise_attach_to=Villain
        ),
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            "You",
            DiscardThisCard,
            against_who="AttachedEnemy"
        ).SetCost(Cost("YR")),
        AbilityFactory.WhenThisBoostAttachTo(
            CardFinder(name="The Hood"),
            otherwise_attach_to=Villain
        ),
    ]

