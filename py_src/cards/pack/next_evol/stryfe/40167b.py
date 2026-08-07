from . import *

# Left to Your Fate

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Stryfe"),
            stalwart=1,
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Identity,
            hand_size=2,
        ),
        AbilityFactory.IncreaseResourceCostToPlay(
            PlayerCard,
            +1,
            "EachPlayer",
        ),
    ]

