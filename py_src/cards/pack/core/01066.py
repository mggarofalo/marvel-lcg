from . import *

# * Hawkeye: Clint Barton

def GetAbilities() -> Sequence['Ability']:

    def hawkeye(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            Minion,
            hawkeye
        ).SetTarget("Trigger")
        .SetCostFunc(CostFunc.Counter("This", 1, 'arrow'))
        # .SetDesc('remove 1 arrow counter from Hawkeye → deal 2 damage to that minion')
        ,
        AbilityFactory.ThisEnterPlayWithCounters(4, 'arrow')
    ]

