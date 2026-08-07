from . import *

# Terrestrial Invasion

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            ResolveTheBadoonShipChargeUpAbility
        ),
    ]

