from . import *

# * Nidhogg

def GetAbilities() -> Sequence['Ability']:


    return [
        AbilityFactory.ThisMinionEngageFirstPlayer(),
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            overkill=True
        ),
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            CardFinder(name="Hall of Nastrond")
        ),
    ]

