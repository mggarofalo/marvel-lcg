from . import *

# Adamantium Lacing

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=2,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="X-23"),
            retaliate=1,
        ),
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="X-23"),
            piercing=True
        ),
    ]

