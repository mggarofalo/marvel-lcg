from . import *

# * Quake: Daisy Johnson

def GetAbilities() -> Sequence['Ability']:

    def quake(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterUnitSchemeEnd(
            AbilityType.Response,
            Minion,
            quake
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Trigger"),
    ]

