from . import *

# * Kang (Scarlet Centurion) (II)

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True
        ),
        WhenDefeatedRemoveSchemeAndJoinAnotherGameArea(THE_PRESENT_FUTURE_WAR)
    ]

