from core import *
from game.card.face import *
from game.world import *
from game.buff import *

class BuffManager:
    def __init__(self, world: 'World') -> None:
        self.buffers: List[Buff] = []

    TB = TypeVar("TB", bound=Buff)
    def RegisterBuffer(self, type: Type['TB']) -> 'TB':
        buff = type()
        self.buffers.append(buff)
        return buff

    def OnRoundEnd(self):
        for buff in self.buffers:
            buff.OnRoundEnd()

    def OnRecordPlayedFace(self, face: 'CardFace'):
        for buff in self.buffers:
            buff.OnRecordPlayedFace(face)

