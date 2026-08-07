from . import *

# * Cap's Helmet

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Captain America")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Captain America"),
            stalwart=1
        ),
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            "You",
            DiscardThisCard,
            against_who=CardFinder(name="Captain America"),
        ).SetCost(Cost("3", same_type=True)),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
    ]

