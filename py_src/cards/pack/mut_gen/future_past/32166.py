from . import *

# * Nimrod

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitCannotTakeMoreDamageEachPhase(
            AbilityType.NonKeyword,
            "This",
            3
        ),
    ]

