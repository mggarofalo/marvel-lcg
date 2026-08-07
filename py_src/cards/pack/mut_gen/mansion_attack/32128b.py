from . import *

# The Basketball Court B

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(card_type=Ally|Minion),
            toughness=1,
        ),
        *AttackOnXaviers(),
    ]

