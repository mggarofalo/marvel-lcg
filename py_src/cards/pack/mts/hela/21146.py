from . import *

# * Nightsword

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Hela")
        ),
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Hela"),
            piercing=True,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            AttachThisToHela
        ),
    ]

