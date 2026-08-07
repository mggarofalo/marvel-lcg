from . import *

# Photon Speed

def GetAbilities() -> Sequence['Ability']:

    def photon_speed(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.form.ChangeEnergyForm(effect, "Photon")
        this.RemoveThreatFromSchemes(effect.targets, 4, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            photon_speed,
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetIgnoreKeyword("Crisis",
            condition=lambda effect:
                effect.GetInitiator().form.IsInEnergyForm("Photon")
        ),
    ]

