from . import *

# * Ronan the Accuser

def GetAbilities() -> Sequence['Ability']:

    return [
        GiveAdditionalBoostCardIfYouControlPowerStone(),
    ]

