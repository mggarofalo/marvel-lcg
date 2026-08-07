from . import *

# * Green Goblin (III)

def GetAbilities() -> Sequence['Ability']:

    return [
        PlaceThreatOnMainSchemeAfterAttacksAndDamageYou(2),
        DealEncounterCardsToEachPlayer(3),
    ]

