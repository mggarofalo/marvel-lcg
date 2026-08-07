from . import *

# * Collector

def GetAbilities() -> Sequence['Ability']:

    return [
        PutCardIntoTheCollectionInsteadDiscard(False)
    ]

