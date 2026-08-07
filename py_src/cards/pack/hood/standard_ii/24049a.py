from . import *

# Formidable Foe

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.StandardModeOnly(),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Villain,
            steady=1,
        )
    ]

