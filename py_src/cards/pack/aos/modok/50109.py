from . import *

# Flying Upgrade

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Adaptoid"),
            scheme=1,
            incite=1,
            trait="AERIAL",
        ),
    ]

