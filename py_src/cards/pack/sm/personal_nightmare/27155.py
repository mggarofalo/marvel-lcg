from . import *

# Fool's Paradise

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Identity,
            hand_size=2,
        )
    ]

