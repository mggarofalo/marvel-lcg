from . import *

# Army of Ants

def GetAbilities() -> Sequence['Ability']:

    def army_of_ants(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            army_of_ants,
            conditions=[
                lambda effect, message:
                    Condition.YouAreInHeroFrom(effect, "TINY")
            ],
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Enemy),
    ]

