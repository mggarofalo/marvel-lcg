from . import *

# Master Mold's Agenda B

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("SENTINEL", Minion),
            guard=1,
        ),
    ]

