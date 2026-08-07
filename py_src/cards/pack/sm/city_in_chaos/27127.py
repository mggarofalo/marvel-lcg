from . import *

# Panic in the Streets

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("LOCATION", Support),
            treat_as_blank=True,
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("PERSONA", Support),
            treat_as_blank=True,
        ),
    ]

