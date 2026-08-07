from . import *

# * George Stacy

def GetAbilities() -> Sequence['Ability']:

    def george_stacy(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.PlaceCardHere(effect.targets, False, effect)


    return [
        *AbilityFactory.AttachedCardCanPlayLikeInHand(
            CardFinder(card_type=Event),
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            george_stacy,
            conditions=[
                lambda effect, message:
                    effect.this.GetPlacedCardArea().GetSize() < 3
            ]
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Event, from_where=["YourHandCards"]),
    ]

