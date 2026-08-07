from . import *

# * Jocasta

def GetAbilities() -> Sequence['Ability']:

    def jocasta(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.PlaceCardHere(effect.targets, False, effect)


    return [
        *AbilityFactory.AttachedCardCanPlayLikeInHand(
            CardFinder(card_type=Event),
        ),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            jocasta
        ).SetTarget(Event, trait="DEFENSE", from_where=["YourDiscardPile"]),
    ]

