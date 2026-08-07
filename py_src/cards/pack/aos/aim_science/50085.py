from . import *

# Mad Science

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("A.I.M", Minion),
            acceleration_icon=1,
        ),
    ]

