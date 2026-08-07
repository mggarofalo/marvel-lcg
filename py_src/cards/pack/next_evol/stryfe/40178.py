from . import *

# Psychic Override

def GetAbilities() -> Sequence['Ability']:

    def psychic_override_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        card_type = player.DeclareCardType()
        player.DiscardAllHands(effect, CardFinder(non_card_type=card_type))

        player.DrawUp("Max", effect)

        size = player.hand_cards.FindCardSize(CardFinder(card_type=card_type))
        this.PlaceThreatOnSchemes("MainScheme", size, effect)

    def psychic_override_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        player.DiscardHandCards((1, 1), effect)
        player.DrawUp(1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            psychic_override_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            psychic_override_boost
        ),
    ]

