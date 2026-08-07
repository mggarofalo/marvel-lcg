from . import *

# * Ayo

def GetAbilities() -> Sequence['Ability']:

    def ayo(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)

    return [
        AfterThisUsesBasicPowerResolveSpecialOnAnotherDoraMilajeAlly(),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            ayo
        ).SetTarget(Enemy),
    ]

