from . import *

# * Skurge

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisMinionEngageFirstPlayer(),
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True
        ),
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            CardFinder(name="Gjallerbru")
        ),
    ]

