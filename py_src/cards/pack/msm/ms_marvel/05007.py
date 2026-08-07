from . import *

# * Bruno Carrelli

def GetAbilities() -> Sequence['Ability']:

    def bruno_carrelli(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.PlaceCardHere(effect.targets, False, effect)

    def bruno_carrelli_gain(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GainCard(effect.targets, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            bruno_carrelli
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(from_where=["YourHandCards"]),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            bruno_carrelli_gain
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("ThisAttachedHereCard", range=(1, 3)),
    ]

