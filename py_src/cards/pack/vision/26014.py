from . import *

# * Protector: Alexis

def GetAbilities() -> Sequence['Ability']:

    def protector(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.ReduceDamage(1, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            "This",
            protector
        ).SetCost(Cost("B"))
        .LimitOncePerRound(),
    ]

