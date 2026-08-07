from . import *

# * Gambit: Remy LeBeau

def GetAbilities() -> Sequence['Ability']:

    def gambit(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)

    return [
        AbilityFactory.ThisEnterPlayWithCounters(
            3,
            'charge',
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.Interrupt,
            "This",
            gambit
        ).SetCostFunc(CostFunc.Counter("This", 1, 'charge'))
        .SetTarget(Enemy),
    ]

