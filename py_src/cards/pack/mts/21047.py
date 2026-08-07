from . import *

# * Quasar: Wendell Vaughn

def GetAbilities() -> Sequence['Ability']:

    def quasar(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            quasar
        ).SetTarget(Scheme2, range="All"),
    ]

