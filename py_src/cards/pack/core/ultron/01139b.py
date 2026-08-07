from . import *

# Countdown to Oblivion - 3B

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            "This"
        )
    ]

