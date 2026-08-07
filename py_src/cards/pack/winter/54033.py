from . import *

# S.H.I.E.L.D. Deputy

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            Friend
        ).SetPlay(only_if_your_identity_has_trait="S.H.I.E.L.D"),
        *AbilityFactory.GiveKeywordToAttached(
            health=1,
            trait="S.H.I.E.L.D"
        ),
    ]

