from . import *

# * Vision

def GetAbilities() -> Sequence['Ability']:

    def vision(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.form.ChangeMassForm(effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            vision
        ).SetName("Density Manipulation")
        .LimitOncePerRound(),
    ]

