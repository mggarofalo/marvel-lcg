from . import *

# * Field Commander

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "You",
            flag="YOU_TAKE_THE_FIRST_TURN_DURING_THE_PLAYER_PHASE",
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(card_type=Upgrade, attach_to=CardFinder(card_type=Minion), set_name="Cyclops"),
            temporary=-1,
        ),
    ]

