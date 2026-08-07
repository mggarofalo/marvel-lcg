from . import *

# * Red Skull

def GetAbilities() -> Sequence['Ability']:

    return [
        RedSkullGetsATKForEachSideSchemeInPlay(),
    ]

