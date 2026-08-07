from . import *

# * Captain America's Shield

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            defense=+1,
            retaliate=+1,
        ),
    ]

