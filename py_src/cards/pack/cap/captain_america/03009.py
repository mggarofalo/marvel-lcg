from . import *

# * Captain America's Shield

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Captain America"),
            defense=1,
            retaliate=1,
        )
    ]
