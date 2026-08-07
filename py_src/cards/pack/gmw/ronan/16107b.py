from . import *

# "Take What Is Mine"

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            "This",
            conditions=[
                lambda effect, message:
                    IsControlPowerStone(Worlds.FindVillain(effect))
            ]
        ),
    ]

