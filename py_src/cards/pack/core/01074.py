from . import *

# Inspired

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(Ally),
        *AbilityFactory.GiveKeywordToAttached(
            Ally,
            thwart=1,
            attack=1,
        ),
    ]

