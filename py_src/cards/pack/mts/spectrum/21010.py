from . import *

# Speed of Light

def GetAbilities() -> Sequence['Ability']:

    def speed_of_light(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.form.ChangeEnergyForm(effect)
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            speed_of_light
        ).SetPlay().SetLabel(),
    ]

