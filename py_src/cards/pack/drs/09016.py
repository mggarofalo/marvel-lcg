from . import *

# Momentum Shift

def GetAbilities() -> Sequence['Ability']:

    def momentum_shift(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            momentum_shift,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetHero().CanHeath()
            ],
        ).SetPlay().SetLabel('attack')
        .SetCostFunc(CostFunc.Heal(2, "YourHero"))
        .SetTarget(Enemy)
    ]

