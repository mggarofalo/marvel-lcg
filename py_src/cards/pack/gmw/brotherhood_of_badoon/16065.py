from . import *

# Badoon Engineer

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.ForcedResponse,
            "This",
            "You",
            ResolveTheBadoonShipChargeUpAbility
        ),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            ResolveTheBadoonShipChargeUpAbility,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            ResolveTheBadoonShipChargeUpAbility
        ),
    ]

