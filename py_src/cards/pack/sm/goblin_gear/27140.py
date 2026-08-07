from . import *

# Limitless Supply

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("TECH", Attachment),
            surge=1,
        )
    ]

