from . import *

# The Power of the Mind

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.DoubleResourcesThisGenerates(
            CardFinder2("PSIONIC")
        ),
    ]

