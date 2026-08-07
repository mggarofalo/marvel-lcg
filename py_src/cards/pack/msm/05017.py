from . import *

# Energy Barrier

def GetAbilities() -> Sequence['Ability']:

    def energy_barrier(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.PreventDamage(1, effect)
        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            "You",
            energy_barrier
        ).SetCostFunc(CostFunc.Counter("This", 1, 'reflection'))
        .SetTarget(Enemy),
    ]

