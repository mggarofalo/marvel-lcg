from . import *

# * Wrecker

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.UnitGetATKWhileAttacking(
            AbilityType.NonKeywordStar,
            "This",
            None,
            2,
            is_undefended_attack=True
        )
    ]

