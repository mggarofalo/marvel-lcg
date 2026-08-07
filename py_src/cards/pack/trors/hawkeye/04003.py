from . import *

# * Hawkeye's Quiver

def GetAbilities() -> Sequence['Ability']:

    def search_top_5_cards_and_attach(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.PlayerDeckTop(
            effect,
            initiator,
            5,
            card_type=Event,
            trait="ARROW",
        )
        if face:
            this.PlaceCardHere(face, True, effect)

    return [
        *AbilityFactory.AttachedCardCanPlayLikeInHand(
            CardFinder2("ARROW", Event),
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            search_top_5_cards_and_attach
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]
