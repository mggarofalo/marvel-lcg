from . import *

# Wave Bracers

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            attack=-1,
            defense=+1,
            retaliate=1,
            steady=1,
        ),
    ]

