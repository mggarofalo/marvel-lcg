from . import *

# * Spectrum: Monica Rambeau

def GetAbilities() -> Sequence['Ability']:

    def spectrum(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.TuckCardUnderHere(effect.targets, effect)
        for face in effect.targets:
            if HasResourceIcon.IsType(face):
                if face.printed_resource.b or face.printed_resource.g:
                    this.GainThwart(+2, effect)
                if face.printed_resource.r or face.printed_resource.g:
                    this.GainAttack(+2, effect)
                if face.printed_resource.y or face.printed_resource.g:
                    this.GainHealth(2, effect)

    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            "This",
            spectrum
        ).SetTarget("UsedToPayForThis"),
    ]

