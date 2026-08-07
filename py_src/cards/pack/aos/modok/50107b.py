from . import *

# * Sarah Garza

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            overkill=True,
            ranged=True
        ),
        FlyingInhumanLeavesPlay(),
    ]

