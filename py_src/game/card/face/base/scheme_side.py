from game.card.face.base import *
from game.card.face import *
from game.ability import *
from game.message import *
from game.player import *
from cards.paper import Paper

class SchemeSide2(HasAccelerationIcon, Scheme2):
    @override
    def __init__(self, paper: 'Paper') -> None:
        super().__init__(paper)

    @override
    def OnPutIntoPlay(self, message: 'Message.WhenCardPutIntoPlay') -> 'bool':
        from game.operate.faces import Faces
        world = self.card.world
        if self.card.area == world.area_schemes_side or \
            Faces.MoveAllTo([self], world.area_schemes_side, message.by_effect, target_game_area=message.target_game_area):
            return super().OnPutIntoPlay(message)
        else:
            return False

    def IgnoreKeywordUntilRoundEnd(self, by_effect: 'Effect', *keywords: 'CardFace.IGNORE_KEY'):
        for keyword in keywords:
            self.SetIgnoreKeyword(+1, keyword, by_effect)

        from cards.pack import RunAt
        def action():
            for keyword in keywords:
                self.SetIgnoreKeyword(-1, keyword, by_effect)
        RunAt.RoundEnd(by_effect, action)

