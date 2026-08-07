from . import *

# Tech Gauntlets

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            most_remaining_hp=True,
            if_cannot_gain_surge=True,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            health=3,
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedMinion",
            overkill=True
        )
    ]

