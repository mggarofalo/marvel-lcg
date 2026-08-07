from . import *

# * Dogpool: Wilson

def GetAbilities() -> Sequence['Ability']:

    def dogpool(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            dogpool
        ).SetTarget(Enemy),
    ]

