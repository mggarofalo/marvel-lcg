from . import *

# * Mister Sinister

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisMinionEngageFirstPlayer()
    ]

