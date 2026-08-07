from . import *

# Symbiote Suit

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Identity,
            attack=1,
            thwart=1,
            defense=1,
            recover=1,
            hand_size=1,
            health=10,
        ),
    ]

