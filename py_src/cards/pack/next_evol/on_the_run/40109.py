from . import *

# Pure Force

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeywordWhileFaceIsInPlay(
            CardFinder(name="Blockbuster"),
            crisis=1,
        ),
        AbilityFactory.ThisGainKeywordWhileFaceIsInPlay(
            CardFinder(name="Chimera"),
            amplify=1,
        ),
    ]

