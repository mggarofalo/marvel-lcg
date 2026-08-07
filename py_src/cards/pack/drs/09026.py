from . import *

# * The Sorcerer Supreme

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
        ).SetPlay(only_if_your_identity_has_trait="MYSTIC"),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            hand_size=1,
        )
    ]

