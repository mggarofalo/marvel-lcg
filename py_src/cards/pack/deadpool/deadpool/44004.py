from . import *

# Maximum Effort

def GetAbilities() -> Sequence['Ability']:

    def maximum_effort(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        damage = effect.cost_func.Get(CostFunc.TakeDamageUpToHealth).return_damage
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            maximum_effort
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
        .SetCostFunc(CostFunc.TakeDamageUpToHealth("YourHero")),
    ]

