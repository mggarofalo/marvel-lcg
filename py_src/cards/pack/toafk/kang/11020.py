from . import *

# Depowered

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.PlayersCannotPlayCardWhile(
            "You",
            CardFinder(card_class="IdentitySpecific"),
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Discard(
            "YourHandCards",
            card_class="IdentitySpecific",
        ))
    ]

