from . import *

# * The Triskelion

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "You",
            ally_limit=1,
        )
    ]

