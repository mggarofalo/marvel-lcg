from . import *

# * Hercules

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ReduceCostToPlayThis(
            1,
            each_minion_engaged_with_you=True
        ),
    ]

