from . import *
from cards.pack.mojo import DiscardEachSettingAndGainSurge

# Mojo Runner

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(card_type=Minion|Ally),
            toughness=1,
        ),
        DiscardEachSettingAndGainSurge()
    ]

