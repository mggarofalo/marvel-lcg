from . import *

# The Hard Call

def GetAbilities() -> Sequence['Ability']:

    def the_hard_call(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        damage = FacesCounter.GetPrintedCost(effect.cost_func.Get(CostFunc.Discard).return_discarded_cards)
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            the_hard_call
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Discard(trait="S.H.I.E.L.D", card_type=Support, from_where=["YouControlCards"]))
        .SetTarget(Enemy, range="All"),
    ]

