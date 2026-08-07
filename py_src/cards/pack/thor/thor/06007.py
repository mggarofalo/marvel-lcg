from . import *

# * Asgard

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Identity,
            control_by="You",
            hand_size=1,
        ),
    ]

