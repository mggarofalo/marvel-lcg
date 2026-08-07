from . import *

# Practiced Defense

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(Enemy),
        *AbilityFactory.GiveKeywordToAttached(
            Enemy,
            attack=-1,
        ),
    ]

