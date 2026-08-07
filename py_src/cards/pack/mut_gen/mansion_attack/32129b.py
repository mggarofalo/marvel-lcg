from . import *

# The Courtyard B

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "Character",
            attack=1,
        ),
        *AttackOnXaviers(),
    ]

