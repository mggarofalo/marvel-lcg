from . import *

# Warning

def GetAbilities() -> Sequence['Ability']:

    def warning(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.ReduceDamage(1, effect)

    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            Hero,
            warning,
        ).SetPlay()
    ]

