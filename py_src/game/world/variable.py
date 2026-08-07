from core import *

from game.player import *
from game.card.face import *

class Variable:

    def __init__(self) -> None:
        self.dic: Dict[str, Any] = {}

    def SetStr(self, name: str, value: str):
        self.dic[name] = value

    def GetStr(self, name: str) -> str:
        return self.dic[name]

    def HasKey(self, name: str) -> bool:
        return name in self.dic

    def SetFace(self, name: str, value: 'CardFace'):
        self.dic[name] = value

    def GetFace(self, name: str) -> 'CardFace':
        return self.dic[name]

    def AddFace(self, name: str, value: 'CardFace'):
        if name not in self.dic:
            self.dic[name] = []
        self.dic[name].append(value)

    def GetFaces(self, name: str) -> List['CardFace']:
        if name not in self.dic:
            return []
        return self.dic[name]

