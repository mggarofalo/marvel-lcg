from . import *

# Finesse

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.HeroResource,
            Resources("G"),
            CardFinder(card_class="Aspect"),
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

