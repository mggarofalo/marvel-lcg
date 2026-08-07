from . import *

# The Hood's Mantle

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="The Hood")
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YBR")),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="The Hood"),
            retaliate=1,
            steady=1,
        ),
    ]

