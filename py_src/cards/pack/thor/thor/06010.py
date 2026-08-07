from . import *

# * Thor's Helmet

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=5,
        ),
    ]

