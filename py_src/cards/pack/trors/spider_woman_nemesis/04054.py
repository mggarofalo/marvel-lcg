from . import *

# * The Viper

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Identity,
            control_by="EngagedPlayer",
            hand_size=-1,
        ),
    ]

