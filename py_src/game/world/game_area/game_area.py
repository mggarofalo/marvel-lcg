from core import *
from game.object import Object
from game.card import *
from game.player import *
from game.world import *

class GameArea(Object):
    def __init__(self, world: 'World') -> None:
        super().__init__('game_area', world)

    def AddPlayer(self, player: 'Player') -> None:
        from game.message import Message
        from game.operate.worlds import Worlds

        game_area = player.GetIdentity().card.game_area

        for face in Worlds.GetSideSchemes(game_area):
            self.AddCard(face.card)
        for face in player.GetControlCharacters():
            self.AddCard(face.card)
        for face in player.supports.Get():
            self.AddCard(face.card)
        for face in player.obligations_area.Get():
            self.AddCard(face.card)
        for face in player.GetEngagedMinions():
            self.AddCard(face.card)
        for face in player.player_deck.Get():
            self.AddCard(face.card)
        for face in player.discard_pile.Get():
            self.AddCard(face.card)
        for face in player.hand_cards.Get():
            self.AddCard(face.card)
        for face in player.dealt_encounter_cards.Get():
            self.AddCard(face.card)
        for face in player.additional_deck.Get():
            self.AddCard(face.card)
        for face in player.additional_discard_pile.Get():
            self.AddCard(face.card)
        message = Message.GameAreaAddPlayer(player)
        message.Send()

    def AddCard(self, card: 'Card') -> None:
        assert card.game_area != self
        if card.IsOnField():
            card.face.OnLeaveGameArea(card.game_area)
        card.game_area = self
        status = card.components.status
        for face in card.face.GetInventoryDeck().Get() + card.face.GetPlacedCardArea().Get() + status.GetDeck().Get(True):
            self.AddCard(face.card)
        if card.IsOnField():
            card.face.OnEnterGameArea(self)

    def FindAllPlayers(self) -> List['Player']:
        players: List['Player'] = []
        for player in self.world.const_players:
            if player.GetGameArea() == self:
                players.append(player)
        return players

    def __repr__(self) -> str:
        return str(self.object_id)

    # def __eq__(self, other: 'GameArea') -> bool:
    # def __eq__(self, other: object) -> bool:
    #     return type(other) is GameArea and self.object_id == other.object_id

