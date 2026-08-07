from . import *

# Ops Room

def GetAbilities() -> Sequence['Ability']:

    def ops_room(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        message.PreventDamage(1, effect)

        this.RemoveThreatFromSchemes(effect.targets2, 1, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            Friend,
            ops_room,
            while_defending=True
        ).SetCostFunc(CostFunc.Counter("This", 1, 'alert'))
        .SetTarget2(Scheme2),
    ]

