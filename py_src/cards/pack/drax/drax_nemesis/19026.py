from . import *

# Cull the Weak

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Enemy,
            attack=2,
        )
    ]

