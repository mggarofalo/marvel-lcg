from . import *

# Beat Cop

def GetAbilities() -> Sequence['Ability']:

    def beat_cop(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        value = Faces.RemoveTokensOn(effect.targets, 1, 'threat', effect)
        if value:
            Faces.PlaceTokensOn([this], value, 'threat', effect)

    def beat_cop_deal(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        damage = this.GetTokens('threat')
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            beat_cop
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Scheme2),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            beat_cop_deal
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Discard("This"))
        .SetTarget(Minion),
    ]

