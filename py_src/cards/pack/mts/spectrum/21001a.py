from . import *

# Spectrum

def GetAbilities() -> Sequence['Ability']:

    def spectrum(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.form.ChangeEnergyForm(effect)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.ForcedResponse,
            "You",
            spectrum,
            to_this_form=True,
        ),
    ]

