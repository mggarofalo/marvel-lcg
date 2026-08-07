from . import *

# Stand Together

def GetAbilities() -> Sequence['Ability']:

    def stand_together(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = message.PreventDamage("All", effect)
        this.DealDamage(effect.targets, value, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            Friend,
            stand_together,
            is_from_attack=True
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Exhaust(Select.Alliance(["AVENGER", "GUARDIAN"])))
        .SetTarget("Attacker"),
    ]

