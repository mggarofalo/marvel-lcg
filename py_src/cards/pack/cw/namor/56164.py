from . import *

# Ruler of Atlantis

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            CardFinder2("ATLANTIS", Minion),
        ),
    ]

