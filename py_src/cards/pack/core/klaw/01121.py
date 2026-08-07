from . import *

# Weapons Runner

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay,
        )
    ]

