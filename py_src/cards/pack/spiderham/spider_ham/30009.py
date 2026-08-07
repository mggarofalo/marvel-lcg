from . import *

# Cartoon Physics

def GetAbilities() -> Sequence['Ability']:

    def cartoon_physics(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        WiggleYourBody()
        value = message.will_take_damage - 1
        message.PreventDamage(value, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            "You",
            cartoon_physics,
            at_least_damage=2,
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("This"),
    ]

