from . import *

# The Cafeteria B

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "Character",
            retaliate=1,
        ),
        *AttackOnXaviers(),
    ]

