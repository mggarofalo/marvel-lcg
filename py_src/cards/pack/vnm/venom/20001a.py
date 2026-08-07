from . import *

# * Venom

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisGainKeyword(
            control_additional_restricted=CardFinder(card_type=Upgrade),
        ),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("G")
        ).SetCostFunc(CostFunc.TakeDamage(1, "YourIdentity"))
        .SetName("Symbiotic Bond")
        .LimitOncePerPhase(),
    ]

