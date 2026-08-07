from . import *

# * Thanos's Armor

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Thanos")
        ),
        AbilityFactory.ReduceCharacterTakeDamageBy(
            CardFinder(name="Thanos"),
            1,
        ),
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            Hero,
            DiscardThisCard,
            against_who=CardFinder(name="Thanos"),
        ).SetCost(Cost("YR")),
    ]

