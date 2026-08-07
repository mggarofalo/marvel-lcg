from . import *

# Practiced Plan

def GetAbilities() -> Sequence['Ability']:

    def practiced_plan(effect: 'Effect', message: 'Message.AfterCardDiscard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ReturnToHand(effect.targets, initiator, effect)


    return [
        AbilityFactory.AfterCardDiscard(
            AbilityType.Response,
            CardFinder2("PREPARATION"),
            practiced_plan,
            control_by_player="You",
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().discard_pile == message.trigger.card.area
            ]
        ).SetTarget("Trigger")
        .SetCostFunc(CostFunc.Discard("This")),
    ]

