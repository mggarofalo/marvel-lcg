from . import *

# * MACH-IV

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.UnitCannotDefend(
            CardFinder(non_trait="AERIAL"),
            "This",
            cannot_trigger_defense_ability=True,
        ),
    ]

