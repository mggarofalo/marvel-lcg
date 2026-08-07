from . import *

# Routed

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.StandardModeOnly(),
        Routed()
    ]

