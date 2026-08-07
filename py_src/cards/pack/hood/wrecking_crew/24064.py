from . import *

# Top Talent

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Villain,
            retaliate=1,
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("ELITE", Minion),
            retaliate=1,
        ),
    ]

