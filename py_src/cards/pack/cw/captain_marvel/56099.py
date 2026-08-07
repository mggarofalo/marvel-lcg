from . import *

# * Captain Marvel's Helmet

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Captain Marvel"),
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Captain Marvel"),
            stalwart=1,
        ),
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            "You",
            DiscardThisCard,
            against_who=CardFinder(name="Captain Marvel"),
        ).SetCost(Cost("3", same_type=True)),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
    ]

