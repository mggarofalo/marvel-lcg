from . import *

# Thruster Boots

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            thwart=1,
            trait="AERIAL",
        )
    ]

