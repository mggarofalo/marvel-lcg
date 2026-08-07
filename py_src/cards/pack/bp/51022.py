from . import *

# * Aneka

def GetAbilities() -> Sequence['Ability']:

    def aneka(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 1, effect)


    return [
        AfterThisUsesBasicPowerResolveSpecialOnAnotherDoraMilajeAlly(),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            aneka
        ).SetTarget(Scheme2),
    ]

