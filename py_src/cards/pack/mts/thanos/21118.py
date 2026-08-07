from . import *

# * Thanos's Helmet

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Thanos")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Thanos"),
            retaliate=1,
        ),
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            Hero,
            DiscardThisCard,
            against_who=CardFinder(name="Thanos"),
        ).SetCost(Cost("BR")),
    ]

