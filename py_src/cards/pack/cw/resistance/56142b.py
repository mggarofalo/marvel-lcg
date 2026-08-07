from . import *

# Secret Avengers

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.UnitGetATKWhileAttacking(
            AbilityType.NonKeyword,
            "EnemyLeader",
            None,
            1,
            is_undefended_attack=True,
        ),
        *AbilityFactory.UnitGetATKWhileAttacking(
            AbilityType.NonKeyword,
            "EnemyLeader",
            None,
            1,
        ),
    ]

