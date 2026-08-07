from . import *

# S.H.I.E.L.D. Patrol

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Minion,
            guard=1
        ),
    ]

