from . import *

# Government Contractor

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Identity,
            trait="UNREGISTERED"
        ),
    ]

