from . import *

# Enraged

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(Ally),
        *AbilityFactory.GiveKeywordToAttached(
            Ally,
            attack=2,
            attack_consequential_damage=1,
        )
    ]

