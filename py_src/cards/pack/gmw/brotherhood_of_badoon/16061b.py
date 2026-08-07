from . import *

# Terrestrial Invasion

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AfterResolveVillainPhaseStep(
            AbilityType.ForcedResponse,
            1,
            ResolveTheBadoonShipChargeUpAbility
        ),
        ExhaustMilanoRemoveThreatFromThisScheme(3)
    ]


