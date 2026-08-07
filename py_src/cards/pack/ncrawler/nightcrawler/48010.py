from . import *

# 'Port Away

def GetAbilities() -> Sequence['Ability']:

    def port_away(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()

        identity.ChangeToOtherForm(effect)
        Faces.ReadyAll([identity], effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            port_away
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Discard("YourHandCards", name="Bamf!"))
    ]

