from . import *

# Touched

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Rogue"),
            overkill=True,
            conditions=[
                lambda effect, message:
                    TouchedAttachedTo(effect, Minion)
            ]
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Rogue"),
            condition=lambda effect:
                TouchedAttachedTo(effect, Villain),
            retaliate=1,
            change_on_event=OnEvent.AttachedMove("This"),
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Rogue"),
            condition=lambda effect:
                TouchedAttachedTo(effect, Ally),
            trait="AERIAL",
            change_on_event=OnEvent.AttachedMove("This"),
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Rogue"),
            condition=lambda effect:
                TouchedAttachedTo(effect, Hero),
            stalwart=1,
            change_on_event=OnEvent.AttachedMove("This"),
        ),
    ]

