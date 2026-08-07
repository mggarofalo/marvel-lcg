from core import *
from game.card.face import *
from game.ability import *
from game.message import *
from game.player import *

@final
class Insert(FinalType):
    # @override
    # def OnPutIntoPlay(self, message: 'Message.WhenCardPutIntoPlay') -> 'bool':
    #     from game.operate.faces import Faces
    #     world = self.card.world
    #     if Faces.MoveAllTo([self], world.area_insert, message.by_effect):
    #         return super().OnPutIntoPlay(message)
    #     else:
    #         return False
    pass

@final
class Challenge(FinalType):
    @override
    def OnPutIntoPlay(self, message: 'Message.WhenCardPutIntoPlay') -> 'bool':
        from game.operate.faces import Faces
        world = self.card.world
        if Faces.MoveAllTo([self], world.area_rule, message.by_effect):
            return super().OnPutIntoPlay(message)
        else:
            return False

