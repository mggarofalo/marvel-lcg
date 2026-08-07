from . import *

# Leadership Training

def GetAbilities() -> Sequence['Ability']:

    def leadership_training(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            leadership_training
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'training'))
        .SetTarget(Event, card_class="Leadership", from_where=["YourDiscardPile"])
    ]

