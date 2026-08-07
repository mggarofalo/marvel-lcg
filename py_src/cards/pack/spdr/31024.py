from . import *

# Unshakable

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
        ).SetPlay(only_if_your_identity_has_at_least_printed_hp=14),
        *AbilityFactory.GiveKeywordToAttached(
            Identity,
            steady=1,
        ),
    ]

