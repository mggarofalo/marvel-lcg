from . import *

# Honorary Avenger

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            Friend
        ).SetPlay(only_if_your_identity_has_trait="AVENGER"),
        *AbilityFactory.GiveKeywordToAttached(
            Friend,
            health=1,
            trait="AVENGER",
        ),
    ]

