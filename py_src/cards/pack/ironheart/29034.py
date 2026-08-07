from . import *

# * Wasp: Nadia Van Dyne

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="CHAMPION"),
        AbilityFactory.UnitIgnoreKeywordIcons(
            "This",
            guard=True,
            patrol=True,
            crisis=True,
        )
    ]

