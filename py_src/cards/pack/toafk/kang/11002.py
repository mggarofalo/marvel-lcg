from . import *

# * Kang (Immortus) (II)

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            "This",
            while_face_is_in_play=CardFinder(card_type=Minion),
        ),
        WhenDefeatedRemoveSchemeAndJoinAnotherGameArea(THE_CHRONOPOLIS)
    ]

