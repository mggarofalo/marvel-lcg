from . import *

# * Magneto's Armor

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Magneto")
        ),
        AbilityFactory.UnitCannotBeStunned(
            CardFinder(name="Magneto")
        ),
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            Hero,
            DiscardThisCard,
            against_who=CardFinder(name="Magneto")
        ).SetCost(Cost("YBR")),
    ]

