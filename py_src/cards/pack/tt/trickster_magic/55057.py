from . import *

# * Titania

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            overkill=True,
        ),
        ThePlayerWhoDefeatedThisMinionPutsTheSetAsideAllyIntoPlayUnderTheirControl("Titania")
    ]

