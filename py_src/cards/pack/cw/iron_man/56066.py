from . import *

# Rocket Boots

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Iron Man")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Iron Man"),
            trait="AERIAL",
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

