from . import *

# Healing Factor

def GetAbilities() -> Sequence['Ability']:

    def healing_factor(effect: 'Effect', message: 'Message.AfterPhaseBegin') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.HealthUnits(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterPlayerPhaseBegin(
            AbilityType.Response,
            healing_factor
        ).SetTarget("YourIdentity", canbe_heal=True)
        .SetCostFunc(CostFunc.Exhaust("This"))
    ]

