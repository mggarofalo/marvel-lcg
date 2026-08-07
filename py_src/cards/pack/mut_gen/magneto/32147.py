from . import *

# * Magneto's Helmet

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Magneto")
        ),
        AbilityFactory.UnitCannotBeConfused(
            CardFinder(name="Magneto")
        ),
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            Hero,
            DiscardThisCard,
            against_who=CardFinder(name="Magneto")
        ).SetCost(Cost("YBR")),
    ]

