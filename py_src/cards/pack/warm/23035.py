from . import *

# Sidearm

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(Ally),
        *AbilityFactory.GiveKeywordToAttached(
            Ally,
            attack=1,
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedAlly",
            ranged=True
        ),
    ]

