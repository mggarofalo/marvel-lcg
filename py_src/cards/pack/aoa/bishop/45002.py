from . import *

# * Malcolm

def GetAbilities() -> Sequence['Ability']:

    def malcolm(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        faces = effect.cost_func.Get(CostFunc.Discard).return_discarded_cards
        if CardFinder(has_printed_res="R").Checks(faces):
            this.HealthUnits(effect.targets, 1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            malcolm
        ).SetCostFunc(CostFunc.Discard(
            "YourHandCards",
            card_type=Resource,
        ))
        .SetTarget("This", canbe_ready=True)
        .LimitOncePerPhase()
    ]

