from . import *

# * Prehensile Tail

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            control_additional_restricted=CardFinder(card_type=Upgrade),
        ),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("G"),
            for_card=CardFinder(card_type=Event)
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

