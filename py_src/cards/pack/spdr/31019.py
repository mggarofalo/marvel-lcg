from . import *

# Forcefield Generator

def GetAbilities() -> Sequence['Ability']:

    def forcefield_generator(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        value = message.will_take_damage
        value = Faces.RemoveCountersOn([this], value, 'energy', effect)
        if value:
            message.PreventDamage(value, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            "You",
            forcefield_generator
        ),
    ]

