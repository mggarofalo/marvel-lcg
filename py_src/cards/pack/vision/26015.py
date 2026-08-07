from . import *

# * Victor Mancha

def GetAbilities() -> Sequence['Ability']:

    def victor_mancha(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.ReduceDamage(1, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.NonKeyword,
            "This",
            victor_mancha,
            is_from_attack=True
        ),
    ]

