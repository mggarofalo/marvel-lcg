from . import *

# Widow's Bite

def GetAbilities() -> Sequence['Ability']:

    def widows_bite(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)
        Faces.GiveStatus(effect.targets, "Stunned", effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.HeroResponse,
            Minion,
            widows_bite
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("Trigger"),
    ]

