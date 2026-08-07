from . import *

# Sewer Tunnels

def GetAbilities() -> Sequence['Ability']:

    return [
        WhenRevealedDiscardOtherSettingEnviroment(),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "Character",
            retaliate=1,
        ),
    ]

