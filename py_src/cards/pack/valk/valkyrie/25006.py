from . import *

# * Dragonfang

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Valkyrie"),
            attack=1,
        ),
        *AbilityFactory.UnitGetATKWhileAttacking(
            AbilityType.NonKeyword,
            CardFinder(name="Valkyrie"),
            HAS_DEATH_GLOW_FINDER,
            1,
        )
    ]

