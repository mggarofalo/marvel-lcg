from . import *

# Armored Assault

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(card_type=Enemy, with_tough=True),
            attack=3,
            change_on_event=OnEvent.Status(Enemy, 'Tough')
        ),
    ]

