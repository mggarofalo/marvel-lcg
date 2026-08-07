from . import *

# Self-Isolation

def GetAbilities() -> Sequence['Ability']:

    def self_isolation(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        face = Search.PlayerCard(
            effect,
            player,
            include_player_deck=True,
            include_discard_pile=True,
            include_player_hand_cards=True,
            include_player_area=True,
            name="Honey Badger",
        )
        if face:
            this.PlaceCardHere([face], False, effect)
        else:
            Faces.DiscardAll([this], effect)
            player.DealEncounterCards(1, effect)

    def self_isolation_response(effect: 'Effect', message: 'Message.AfterUnitRecovery') -> None:
        this = effect.this.CastTo(Obligation)

        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            self_isolation
        ),
        AbilityFactory.AfterUnitMakeRecovery(
            AbilityType.Response,
            "You",
            self_isolation_response
        ),
    ]

