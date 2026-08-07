from . import *

# X-Gene

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
        ).SetPlay(only_if_your_identity_has_trait="MUTANT"),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("G"),
            CardFinder(card_class="IdentitySpecific", card_type=Event),
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

