from . import *

# Honorary X-Men

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            Friend
        ).SetPlay(only_if_your_identity_has_trait="X-MEN"),
        *AbilityFactory.GiveKeywordToAttached(
            Friend,
            trait="X-MEN",
            health=1,
        ),
    ]

