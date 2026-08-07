from . import *

# Enraged Symbiote

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay,
        )
    ]

