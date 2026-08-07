from . import *

# Mighty Avengers

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisSupportCard(
            under_any_players_control=True
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Ally,
            control_by="You",
            if_each_of_your_characters_has_trait="AVENGER",
            thwart=1,
            attack=1,
            change_on_event=OnEvent.Trait("YouControlCharacter")
        ),
    ]

