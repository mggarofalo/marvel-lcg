from . import *

# * Hawkeye: Clint Barton

def GetAbilities() -> Sequence['Ability']:

    def hawkeye(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.ThisEnterPlayWithCounters(
            4,
            'arrow'
        ),
        AbilityFactory.AfterUnitMakeThwart(
            AbilityType.Response,
            Ally,
            hawkeye,
            is_basic_thwart=True
        ).SetTarget(Minion)
        .SetCostFunc(CostFunc.Counter("This", 1, 'arrow')),
    ]

