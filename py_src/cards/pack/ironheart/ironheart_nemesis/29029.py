from . import *

# Rule by Force

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeywordWhileFaceIsInPlay(
            CardFinder(name="Lucia von Bardas"),
            hazard=1,
        ),
        AbilityFactory.ThisGainKeywordWhileFaceIsNotInPlay(
            CardFinder(name="Lucia von Bardas"),
            acceleration_icon=1,
        ),
    ]

