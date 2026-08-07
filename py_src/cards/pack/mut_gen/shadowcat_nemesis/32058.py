from . import *

# Hellfire Pawn

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay
        ),
    ]

