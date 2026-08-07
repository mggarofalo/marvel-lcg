from . import *

# Tech Theft

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("TECH", PlayerCard),
            treat_as_blank=True,
        )
    ]

