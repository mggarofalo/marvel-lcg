from . import *
from cards.pack.mojo import DiscardEachSettingAndGainSurge

# A Game of Mojo's

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "Player",
            hand_size=1,
        ),
        DiscardEachSettingAndGainSurge(),
    ]

