from . import *

# Animal Ferocity

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Sabretooth")
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YBR")),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Sabretooth"),
            stalwart=1,
        ),
        AbilityFactory.WhenThisBoostAttachTo(
            CardFinder(name="Sabretooth")
        ),
    ]

