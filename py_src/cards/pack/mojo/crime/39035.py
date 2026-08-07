from . import *
from cards.pack.mojo import DiscardEachSettingAndGainSurge

# Dial M for Mojo

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            EncounterCard,
            incite=1,
            not_include_this=True,
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Friend,
            thwart=1,
        ),
        DiscardEachSettingAndGainSurge()
    ]

