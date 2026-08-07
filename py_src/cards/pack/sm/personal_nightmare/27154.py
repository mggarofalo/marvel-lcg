from . import *

# Evil Doppelgänger

def GetAbilities() -> Sequence['Ability']:

    def get_engaged_player_identity_specific_cards_size(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Minion)

        if this.engaged_player:
            faces = this.GetEngagedPlayer().hand_cards.FindCards(card_class="IdentitySpecific")
            ui.extend(faces)
            return len(faces)
        return 0

    def evil_doppelganger_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DrawUp(3, effect)
        player.DiscardRandomHandCards(3, effect)


    return [
        AbilityFactory.ThisGainKeyword(
            get_engaged_player_identity_specific_cards_size,
            attack=1,
            scheme=1,
            change_on_event=OnEvent.HandCards("EngagedPlayer")
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            evil_doppelganger_boost
        ),
    ]

