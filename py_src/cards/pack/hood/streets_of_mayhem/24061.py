from . import *

# Secret Lair

def GetAbilities() -> Sequence['Ability']:

    return [
        WhenRevealedDiscardOtherSettingEnviroment(),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Enemy,
            acceleration_icon=1,
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(card_type=Hero|Ally),
            thwart=1,
        ),
    ]

