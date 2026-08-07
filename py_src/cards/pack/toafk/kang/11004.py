from . import *

# * Kang (Rama-Tut) (II)

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeywordForEachFaceInPlay(
            CardFinder(card_type=Obligation),
            attack=1,
        ),
        WhenDefeatedRemoveSchemeAndJoinAnotherGameArea(THE_REALM_OF_RAMA_TUT)
    ]

