from . import *

# The Brotherhood

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("BROTHERHOOD OF MUTANTS", Minion),
            quickstrike=1,
        ),
    ]

