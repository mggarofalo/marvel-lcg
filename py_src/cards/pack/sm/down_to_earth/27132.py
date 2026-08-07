from . import *

# Friends and Family

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.IncreaseResourceCostToPlay(
            Event,
            +1,
            "You",
            conditions=[
                lambda effect, message:
                    message.GetToPlayer().IsInHeroForm(),
            ]
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Discard(
            "YourHandCards",
            card_class="IdentitySpecific"))
    ]

