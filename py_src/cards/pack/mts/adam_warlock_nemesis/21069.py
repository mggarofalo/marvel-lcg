from . import *

# Zealot of Truth

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            CardFinder(name="Universal Church of Truth", card_type=SchemeSide2),
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay
        ),
    ]

