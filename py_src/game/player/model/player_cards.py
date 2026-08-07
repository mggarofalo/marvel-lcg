from core import *
from game.card.face import *
from game.deck import *
from game.effect import *
from game.player import *

class PlayerCards:
    def GetPlayer(self) -> 'Player':
        from game.player import Player
        return Cast(Player, self)

    def PlaceOnTopAndOrBottomInAnyOrder(self, faces: List['CardFace'], by_effect: 'Effect'):
        player = self.GetPlayer()

        bottoms = faces[:]
        tops = player.AskChooseFaces(
            faces, (0, "All"),
            by_effect,
            peek=True,
            not_move=True,
            prompt="Place on top")
        for top in reversed(tops):
            top.card.MoveToTop(top.card.area, by_effect)
            bottoms.remove(top)
        player.PlaceOnBottomInAnyOrder(bottoms, by_effect)

    # The faces should be from top to bottom
    def PlaceOnBottomInAnyOrder(self, faces: List['CardFace'], by_effect: 'Effect'):
        player = self.GetPlayer()

        # If cancel, it should not change the order
        faces = player.AskChooseFaces(
            faces, "All",
            by_effect,
            peek=True,
            not_move=True,
            prompt="Place on bottom")
        for face in faces:
            face.card.MoveToBottom(face.card.area, by_effect)

    # The faces should be from top to bottom
    def PlaceOnTopInAnyOrder(self, faces: List['CardFace'], by_effect: 'Effect'):
        player = self.GetPlayer()

        # If cancel, it should not change the order
        faces = player.AskChooseFaces(
            faces, "All",
            by_effect,
            peek=True,
            not_move=True,
            prompt="Place on top")
        for face in reversed(faces):
            face.card.MoveToTop(face.card.area, by_effect)

