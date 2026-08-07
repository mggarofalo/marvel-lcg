from . import *

# Mental Detection

def GetAbilities() -> Sequence['Ability']:

    def mental_detection(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        psi_knife = GetYouControlPsiKnife(initiator)
        value = 1 + psi_knife * 2
        this.RemoveThreatFromSchemes(effect.targets, value, effect)

        psi_katana = GetYouControlPsiKatana(initiator)
        initiator.DrawUp(psi_katana, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            mental_detection
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

