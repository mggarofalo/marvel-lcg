from . import *

# Jessica Jones

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeywordForEachFaceInPlay(
            CardFinder(card_type=SchemeSide2),
            thwart=1,
        )
    ]

