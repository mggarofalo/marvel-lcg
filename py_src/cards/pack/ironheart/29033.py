from . import *

# * Bombshell: Lana Baumgartner

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="CHAMPION"),
        AbilityFactory.ThisCanAttack(
            divided_evenly=True
        ),
    ]

