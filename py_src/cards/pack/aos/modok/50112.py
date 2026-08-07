from . import *

# Strong Upgrade

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Adaptoid"),
            trait="BRUTE",
            attack=1,
            toughness=1,
        ),
    ]

