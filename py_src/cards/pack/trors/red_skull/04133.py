from . import *

# Red Skull's Right Hook

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Red Skull")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Red Skull"),
            retaliate=1,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YBR")),
    ]

