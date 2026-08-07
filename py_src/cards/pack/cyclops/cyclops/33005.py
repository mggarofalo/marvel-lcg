from . import *

# Exploit Weakness

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(Enemy),
        AbilityFactory.IncreaseAllDamageUnitTakeBy(
            AbilityType.NonKeyword,
            "AttachedEnemy",
            1,
            is_from_attack=True
        )
    ]

