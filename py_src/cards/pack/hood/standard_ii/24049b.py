from . import *

# Formidable Foe

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ExpertModeOnly(),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Enemy,
            steady=1,
        )
    ]

