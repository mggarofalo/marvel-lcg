from . import *

# Save the Day

def GetAbilities() -> Sequence['Ability']:

    def save_the_day(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        allies = effect.cost_func.Get(CostFunc.Discard).return_discarded_cards
        allies = Filter.ByType(allies, Ally)
        value = FacesCounter.GetPrintedCost(allies)
        this.RemoveThreatFromSchemes(effect.targets, value, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            save_the_day
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Discard("YourAlly"))
        .SetTarget(Scheme2),
    ]

