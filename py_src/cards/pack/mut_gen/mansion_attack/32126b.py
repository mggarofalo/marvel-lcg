from . import *

# The Atrium B

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "Character",
            steady=1,
        ),
        *AttackOnXaviers(),
    ]

