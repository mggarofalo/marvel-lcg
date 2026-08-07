from . import *

# The Power of Flight

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.DoubleResourcesThisGenerates(
            CardFinder2("AERIAL")
        ),
    ]

