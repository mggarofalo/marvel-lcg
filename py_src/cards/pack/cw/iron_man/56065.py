from . import *

# Powered Gauntlets

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Iron Man")
        ),
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Iron Man"),
            ranged=True
        ),
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            "You",
            DiscardThisCard,
            against_who=CardFinder(name="Iron Man"),
        ).SetCost(Cost("YR")),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
    ]

