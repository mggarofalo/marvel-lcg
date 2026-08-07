from . import *

# Destroy Evidence

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            EncounterCard,
            incite=1,
            not_include_this=True,
        ),
    ]

