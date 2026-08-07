from . import *

# Razor Claws

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            highest_printed_hp=True,
            if_cannot_gain_surge=True
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedMinion",
            piercing=True
        ),
    ]

