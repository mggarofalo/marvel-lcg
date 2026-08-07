from . import *

# Go Down Swinging

def GetAbilities() -> Sequence['Ability']:

    def go_down_swinging(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        allies = effect.cost_func.Get(CostFunc.Discard).return_discarded_cards
        allies = Filter.ByType(allies, Ally)
        damage = FacesCounter.GetPrintedCost(allies)
        this.DealDamage(effect.targets, damage, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            go_down_swinging
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Discard("YourAlly"))
        .SetTarget(Enemy),
    ]

