from . import *

# * Titania: Mary MacPherran

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            overkill=True,
            piercing=True
        ),
    ]

