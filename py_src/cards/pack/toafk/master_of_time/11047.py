from . import *

# * Kang (Master of Time)

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeywordForEachFaceInPlay(
            CardFinder(card_type=Obligation),
            scheme=1,
            attack=1,
        ),
    ]

