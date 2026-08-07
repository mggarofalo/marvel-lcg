from . import *

# Time Portal

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.ForcedInterrupt,
            "This",
            ShuffleThisCardIntoEncounter
        )
    ]

