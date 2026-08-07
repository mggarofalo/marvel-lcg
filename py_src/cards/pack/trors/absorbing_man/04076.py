from . import *

# * Absorbing Man

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbsorbingManGainsTraitOfEnvironment()
    ]

