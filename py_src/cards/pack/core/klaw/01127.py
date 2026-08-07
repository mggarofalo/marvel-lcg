from . import *

# The "Immortal" Klaw

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Klaw"),
            health=10,
        )
    ]

