from . import *

# * Wrecker (I)

def GetAbilities() -> Sequence['Ability']:

    return [
        PlaceTheThreatOnHisSideScheme(),
        *AbilityFactory.UnitGetATKWhileAttacking(
            AbilityType.NonKeywordStar,
            "This",
            None,
            2,
            is_undefended_attack=True
        )
    ]

