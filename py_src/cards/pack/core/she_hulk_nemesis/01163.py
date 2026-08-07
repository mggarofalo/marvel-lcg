from . import *

# Genetically Enhanced

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            highest_printed_hp=True,
            if_cannot_gain_surge=True
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            health=3,
        ),
    ]

