from . import *

# Adamantium Skeleton

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=4,
        ),
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Wolverine"),
            is_basic_attack=True,
            piercing=True,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Wolverine"),
            attack=1,
        )
    ]

