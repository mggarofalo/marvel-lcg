from . import *

# * Alpha Flight Station

def GetAbilities() -> Sequence['Ability']:

    def alpha_flight_station(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        size = 1
        if initiator.GetIdentity().IsName("Carol Danvers"):
            size = 2
        initiator.DrawUp(size, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            alpha_flight_station
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Discard("YourHandCards"))
    ]

