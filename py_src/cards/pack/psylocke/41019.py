from . import *

# Directed Force

def GetAbilities() -> Sequence['Ability']:

    def directed_force(effect: 'Effect', message: 'Message.WhenUnitMakeKeyWordAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.DealAdditionalDamage(2, effect)


    return [
        AbilityFactory.WhenUnitMakeKeyWordAttack(
            AbilityType.HeroInterrupt,
            "YourHero",
            directed_force,
        ).SetPlay().LimitOncePerEvent(),
    ]

