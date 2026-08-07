from . import *

# * Ironheart

def GetAbilities() -> Sequence['Ability']:

    def ironheart(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            ironheart
        ).SetName("Maximum Efficiency") \
        .SetCostFunc(CostFunc.Counter("This", 1, 'progress')) \
        .SetTarget(Enemy),
    ]

