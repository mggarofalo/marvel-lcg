from . import *

# Honorary Guardian

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            Friend,
        ).SetPlay(only_if_your_identity_has_trait="GUARDIAN"),
        *AbilityFactory.GiveKeywordToAttached(
            "Character",
            health=1,
            trait="GUARDIAN",
        ),
    ]

