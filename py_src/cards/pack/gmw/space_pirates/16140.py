from . import *

# Sound the Alarms

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Enemy,
            attack=1,
        ),
    ]

