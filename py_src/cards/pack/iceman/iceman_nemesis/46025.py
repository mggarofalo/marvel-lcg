from . import *

# * Pyro

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            indirect_damage=True,
        ),
    ]

