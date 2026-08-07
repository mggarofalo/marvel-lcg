from . import *

# Solid

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.HeroResource,
            Resources("R"),
            for_card=CardFinder(traits=["ATTACK", "DEFENSE"], card_type=Event)
        ).SetCostFunc(CostFunc.Exhaust("This")),
        AbilityFactory.AfterUnitAttackOrDefend(
            AbilityType.Response,
            "You",
            lambda effect, message:
                FlipSolidPhased(effect, message, "Phased"),
        ),
    ]

