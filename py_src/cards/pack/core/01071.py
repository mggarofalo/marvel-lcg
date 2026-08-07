from . import *

# Make the Call

def GetAbilities() -> Sequence['Ability']:

    def make_the_call(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        target = effect.targets[0].CastTo(Ally)
        initiator = effect.GetInitiator()

        target.PutIntoPlay(initiator, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            make_the_call
        ).SetPlay()
        .SetTarget(CardFinder(not_has_that_unique_in_play=True, card_type=Ally), from_where=["PlayersDiscardPile"])
        .SetCostFunc(CostFunc.PayPrintCost("Target"))
    ]

