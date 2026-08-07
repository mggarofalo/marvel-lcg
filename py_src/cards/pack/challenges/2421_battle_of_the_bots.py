from . import *

# Battle of the Bots

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("DRONE", Minion),
            trait="SENTINEL",
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("SENTINEL", Minion),
            trait="DRONE",
        )
    ]

