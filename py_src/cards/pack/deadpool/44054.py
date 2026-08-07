from . import *

# Distraction

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            NonEliteMinion
        ),
        AbilityFactory.EnemyCannotActivate(
            AbilityType.NonKeyword,
            "AttachedMinion"
        ),
    ]

