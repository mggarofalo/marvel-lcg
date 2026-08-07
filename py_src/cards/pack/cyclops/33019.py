from . import *

# Angel: Warren Worthington III

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ReduceCostToPlayThis(
            1,
            your_identity_has_traits=["MUTANT", "X-MEN"],
        )
    ]

