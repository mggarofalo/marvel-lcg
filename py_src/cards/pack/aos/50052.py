from . import *

# Prism Dust

def GetAbilities() -> Sequence['Ability']:

    def prism_dust(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Confused", effect)
        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.HeroResponse,
            Minion,
            prism_dust
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("Trigger"),
    ]

