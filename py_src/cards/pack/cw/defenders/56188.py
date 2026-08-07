from . import *

# The Defenders

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Minion,
            guard=1,
        ),
    ]

