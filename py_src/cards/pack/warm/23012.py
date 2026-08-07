from . import *

# * Black Panther: T'Challa

def GetAbilities() -> Sequence['Ability']:

    def black_panther(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        for face in effect.targets:
            this.PlaceCardHere(face, False, effect)

    return [
        *AbilityFactory.AttachedCardCanPlayLikeInHand(
            CardFinder(card_type=Event),
        ),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            black_panther
        ).SetTarget(Event, card_class="Leadership", from_where=["YourDiscardPile"]),
    ]

