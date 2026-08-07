from . import *

# Retinal Display

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitBasicThwartCanOnlyRemoveThreatFromMostThreatScheme(
            "YourHero",
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            thwart=2,
        ),
        AbilityFactory.UnitIgnoreKeywordIcons(
            "YourHero",
            crisis=True,
            patrol=True
        ),
    ]

