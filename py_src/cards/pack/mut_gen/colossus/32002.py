from . import *

# * Shadowcat: Kitty Pryde

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitIgnoreKeywordIcons(
            "This",
            guard=True,
            patrol=True,
            crisis=True,
        )
    ]

