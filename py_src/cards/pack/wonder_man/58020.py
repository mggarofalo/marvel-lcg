from . import *

# Unified Strike

def GetAbilities() -> Sequence['Ability']:

    def unified_strike(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        allies = effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards
        message.AddThatMatchingPower(allies, effect)
        message.DoesNotTakeConsequentialDamage(effect)


    return [
        AbilityFactory.WhenUnitUseBasicPower(
            AbilityType.HeroInterrupt,
            Ally,
            unified_strike
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Exhaust("YourHero")),
    ]

