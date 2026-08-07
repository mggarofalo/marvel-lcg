from . import *

# * Bulldozer (I)

def GetAbilities() -> Sequence['Ability']:

    return [
        PlaceTheThreatOnHisSideScheme(),
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            overkill=True
        )
    ]

