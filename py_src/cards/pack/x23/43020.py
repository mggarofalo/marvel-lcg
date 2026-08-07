from . import *

# The Direct Approach

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder(is_permanent=False, card_type=SchemeSide2)
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Scheme2,
            assault=1,
        ),
    ]

