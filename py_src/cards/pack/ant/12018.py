from . import *

# Reinforced Suit

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(Ally),
        *AbilityFactory.GiveKeywordToAttached(
            Ally,
            health=2,
        ),
    ]

