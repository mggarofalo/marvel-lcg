from . import *

# * Fandral

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitIgnoreKeywordIcons(
            "This",
            crisis=True,
            while_use_basic_thw=True
        ),
    ]

