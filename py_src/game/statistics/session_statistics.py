from typing import TypeAlias
from core import *
# from engine.lib import Json
from game.card.face import *

KEY_NAME: TypeAlias = Literal['damage_dealt', 'damage_taken', 'thwarted_threat', 'entered_play']

class SessionStatistics:

    def __init__(self) -> None:
        self.dic: Dict[int, Dict[KEY_NAME, int]] = {}
        self.DEFAULT_STATS = dict.fromkeys(get_args(KEY_NAME), 0)

    def Add(self, face: 'CardFace', value: int, name: KEY_NAME):
        if not PlayerCard.IsType(face):
            return
        id = face.card.object_id
        stats = self.dic.setdefault(id, self.DEFAULT_STATS.copy())
        stats[name] += value

    # def Get(self) -> str:
    #     return Json.Dumps(self.dic)

