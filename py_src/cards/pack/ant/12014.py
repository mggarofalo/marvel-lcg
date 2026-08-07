from . import *

# * Stinger

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="AVENGER"),
        *AbilityFactory.ThisNotCountAllyLimit()
    ]

