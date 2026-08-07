from . import *

# Chimichanga Truck

def GetAbilities() -> Sequence['Ability']:

    def chimichanga_truck(effect: 'Effect', message: 'Message.AfterUnitRecovery') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitMakeRecovery(
            AbilityType.Response,
            Identity,
            chimichanga_truck,
            is_basic_recovery=True,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Trigger"),
    ]

