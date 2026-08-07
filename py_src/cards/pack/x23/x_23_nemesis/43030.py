from . import *

# In the Name of Vengence

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Enemy,
            retaliate=1,
        ),
    ]

