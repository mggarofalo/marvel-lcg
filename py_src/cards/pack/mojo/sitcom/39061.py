from . import *

# Family Matters

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Support,
            control_by="You",
            treat_as_blank=True,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        # ).SetCostFunc(CostFunc.Exhaust("YourIdentity")
        ).SetCostFunc(CostFunc.Exhaust(
            card_type=Support|Identity,
            range=(lambda effect: 1+len(effect.GetInitiator().GetControlCardsByType(support=True)), "All"),
            from_where=["YouControlCards"])
        )
    ]

