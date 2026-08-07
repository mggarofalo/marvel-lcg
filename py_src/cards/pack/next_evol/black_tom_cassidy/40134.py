from . import *

# Making Green

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Creeping Willow"),
            surge=1,
        ),
    ]

