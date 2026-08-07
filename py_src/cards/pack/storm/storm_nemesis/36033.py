from . import *

# Switchblade

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            highest_printed_atk=True,
            if_cannot_gain_surge=True,
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedMinion",
            piercing=True
        ),
    ]

