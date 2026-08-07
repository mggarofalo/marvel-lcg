from . import *

# * Green Goblin (II)

def GetAbilities() -> Sequence['Ability']:

    return [
        PlaceThreatOnMainSchemeAfterAttacksAndDamageYou(1),
        DealEncounterCardsToEachPlayer(2),
    ]

