from . import *

# * Okoye

def GetAbilities() -> Sequence['Ability']:

    def okoye(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        for face in effect.targets:
            face.GainUntilPhaseEnd(
                effect,
                thwart=1,
                attack=1,
            )

    return [
        AfterThisUsesBasicPowerResolveSpecialOnAnotherDoraMilajeAlly(),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            okoye
        ).SetTarget("Hero|Ally", trait="WAKANDA"),
    ]

