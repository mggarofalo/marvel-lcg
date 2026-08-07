from . import *

# Strong Inhuman

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.ThisNotCountAllyLimit(),
        FlyingInhumanLeavesPlay(),
    ]

