from . import *

# Pinned Down

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(Minion),
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            attack=-2,
        ),
    ]

