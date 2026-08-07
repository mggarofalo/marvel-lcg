from . import *

# Criminal Underworld

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            CardFinder(card_type=Minion, trait="CRIMINAL"),
        ),
    ]

