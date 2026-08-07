from . import *

# Live Dangerously

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Identity,
            hand_size=2,
        )
    ]

