from . import *

# * Garm

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisMinionEngageFirstPlayer(),
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            CardFinder(name="Gnipahellir")
        ),
    ]

