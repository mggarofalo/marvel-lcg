from . import *

# Retinal Display

def GetAbilities() -> Sequence['Ability']:


    return [
        AbilityFactory.UnitBasicThwartCanOnlyRemoveThreatFromMostThreatScheme(
            "YourHero",
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            thwart=1,
        ),
        AbilityFactory.UnitIgnoreKeywordIcons(
            "YourHero",
            crisis=True
        ),
    ]

