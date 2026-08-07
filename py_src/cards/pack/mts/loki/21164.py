from . import *

# * Loki

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True,
        ),
        Loki()
    ]

