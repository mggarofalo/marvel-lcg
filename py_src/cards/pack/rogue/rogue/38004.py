from . import *

# * Rogue's Jacket

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Rogue"),
            condition=lambda effect:
                TouchedAttachedTo(effect, Friend),
            thwart=1,
            change_on_event=OnEvent.AttachedMove(CardFinder(name="Touched")),
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Rogue"),
            condition=lambda effect:
                TouchedAttachedTo(effect, Enemy),
            attack=1,
            change_on_event=OnEvent.AttachedMove(CardFinder(name="Touched")),
        ),
    ]

