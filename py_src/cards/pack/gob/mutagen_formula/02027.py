from . import *

# Goblin Nation

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay,
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("GOBLIN", Enemy),
            attack=1,
        ),
    ]

