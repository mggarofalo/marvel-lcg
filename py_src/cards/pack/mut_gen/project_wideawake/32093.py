from . import *

# Sentinel Mark IV

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay
        ),
    ]

