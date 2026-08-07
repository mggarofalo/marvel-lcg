from . import *

# "Yoo-Hoo!"

def GetAbilities() -> Sequence['Ability']:

    def yoo_hoo(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = effect.cost_func.Get(CostFunc.TakeDamageUpToHealth).return_damage
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            yoo_hoo
        ).SetPlay().SetLabel('thwart')
        .SetCostFunc(CostFunc.TakeDamageUpToHealth("YourHero"))
        .SetTarget(Scheme2),
    ]

