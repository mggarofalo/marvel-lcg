from . import *

# Holding Cell

def GetAbilities() -> Sequence['Ability']:

    return [
        *HoldingCell(Cost("YY"))
    ]

