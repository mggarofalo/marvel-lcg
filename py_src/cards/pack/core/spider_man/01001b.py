from . import *

# * Peter Parker

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("B")
        ).SetName("Scientist")
        .LimitOncePerRound()
    ]

