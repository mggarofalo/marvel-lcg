from . import *

# Back-Alley Enclave

def GetAbilities() -> Sequence['Ability']:

    return [
        WhenRevealedDiscardOtherSettingEnviroment(),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "Character",
            attack=1,
        ),
    ]

