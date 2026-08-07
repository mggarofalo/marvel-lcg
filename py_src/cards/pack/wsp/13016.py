from . import *

# Boot Camp

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisSupportCard(
            under_any_players_control=True
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Ally,
            control_by="You",
            attack=1,
        )
    ]

