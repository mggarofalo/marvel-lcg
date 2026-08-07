from . import *

# Cybernetic Enhancements

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            if_cannot_gain_surge=True,
        ),
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            "AttachedMinion",
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "AttachedMinion",
            DiscardThisCard
        ),
    ]

