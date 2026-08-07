from . import *

# Warehouse District

def GetAbilities() -> Sequence['Ability']:

    return [
        WhenRevealedDiscardOtherSettingEnviroment(),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "Character",
            steady=1,
        ),
    ]

