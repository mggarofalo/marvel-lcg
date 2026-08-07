from . import *

# Double Life

def GetAbilities() -> Sequence['Ability']:

    def double_life(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()
        identity.ChangeToOtherForm(effect)

        if effect.GetPaidResources().HasColor("R"):
            Faces.ReadyAll([identity], effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            double_life
        ).SetPlay().SetLabel()
        .MaxOnePerRound(),
    ]

