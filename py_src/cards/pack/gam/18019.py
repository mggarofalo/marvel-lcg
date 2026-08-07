from . import *

# * Drax

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="GUARDIAN"),
        *AbilityFactory.UnitCannotAttackTarget(
            "This",
            cannot_attack=Minion
        )
    ]

