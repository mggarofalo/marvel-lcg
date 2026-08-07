from . import *

# * Wasp

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisCanAttack(
            canbe_divided=True
        ),
        AbilityFactory.ThisCanThwart(
            canbe_divided=True
        ),
    ]

