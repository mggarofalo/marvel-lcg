from . import *

# * Loki's Crown

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Loki")
        ),
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            "You",
            DiscardThisCard,
            against_who=CardFinder(name="Loki")
        ).SetCost(Cost("BR")),
        AbilityFactory.WhenThisBoostAttachTo(
            CardFinder(name="Loki")
        ),
    ]

