from . import *

# * Technovirus Purge

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            "This",
            by_character=CardFinder(non_name="Cable"),
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Cable"),
            works_in_the_victory_display=True,
            thwart=1,
            attack=1,
            defense=1,
            trait="PSIONIC",
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Nathan Summers"),
            works_in_the_victory_display=True,
            trait="PSIONIC",
        ),
    ]

