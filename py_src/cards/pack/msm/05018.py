from . import *

# * Lockjaw

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.YouMayPlayCardLikeInHand(
            AbilityType.NonKeyword,
            "This",
            from_where="YourDiscardPile",
            during="YourTurn",
        )
    ]

