from . import *

# * Biokinetic Polymer Suit

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.HeroResource,
            Resources("G"),
            CardFinder(card_type=Event),
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

