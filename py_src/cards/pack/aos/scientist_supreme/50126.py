from . import *

# * Monica Rappaccini

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeywordWhileFaceIsInVictoryDisplay(
            CardFinder(name="Scientist Supreme"),
            villainous=1,
        ),
    ]

