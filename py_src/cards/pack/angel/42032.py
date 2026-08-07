from . import *

# X-Force Recruit

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            Friend
        ).SetPlay(only_if_your_identity_has_trait="X-FORCE"),
        *AbilityFactory.GiveKeywordToAttached(
            "Character",
            health=1,
            trait="X-FORCE",
        ),
    ]

