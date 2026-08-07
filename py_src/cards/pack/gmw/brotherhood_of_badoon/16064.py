from . import *

# * Drang's Spear

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Drang"),
            stalwart=1,
        ),
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Drang")
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("BRR")),
    ]

