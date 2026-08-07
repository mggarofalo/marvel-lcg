from . import *

# Redemption

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.TakeControlOfAttachedMinion("Redeemed Ally"),
    ]

