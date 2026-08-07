from . import *

# Wave Bracers

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            attack=-1,
            defense=+2,
            retaliate=1,
            stalwart=1,
        ),
    ]

