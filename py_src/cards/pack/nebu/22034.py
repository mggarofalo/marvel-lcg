from . import *

# Defensive Training

def GetAbilities() -> Sequence['Ability']:

    def defensive_training(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            defensive_training
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'training'))
        .SetTarget(Event, card_class="Protection", from_where=["YourDiscardPile"]),
    ]

