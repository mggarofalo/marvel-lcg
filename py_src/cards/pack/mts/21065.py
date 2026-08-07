from . import *

# * Martinex: T'Naga

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ReduceCostToPlayThis(
            1,
            your_identity_has_traits=["GUARDIAN"]
        ),
    ]

