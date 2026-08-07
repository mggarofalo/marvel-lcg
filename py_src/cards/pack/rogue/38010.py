from . import *

# * Iceman: Bobby Drake

def GetAbilities() -> Sequence['Ability']:

    def iceman(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)


    return [
        AbilityFactory.ThisEnterPlayWithCounters(
            3,
            'freeze',
        ),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            Minion,
            iceman
        ).SetCostFunc(CostFunc.Counter("This", 1, 'freeze'))
        .SetTarget("Trigger"),
    ]

