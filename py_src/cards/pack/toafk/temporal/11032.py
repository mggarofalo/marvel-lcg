from . import *

# Tyrannosaurus Rex

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True
        )
    ]

