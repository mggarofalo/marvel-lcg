from . import *

# Solid-Sound Body

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Klaw")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Klaw"),
            retaliate=1,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YBR"))
    ]
