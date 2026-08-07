from core import *
from game.card.face.base import *
from game.card.face import *
from game.message import *
from game.ability import *
from cards.paper import Paper

class Friend(Unit2):
    @override
    def __init__(self, paper: 'Paper') -> None:
        super().__init__(paper)

    def GetBasicPowerEffects(self, powers: List["CardFace.BASIC_POWER"]) -> List['Effect']:
        powers_set = set(powers)
        return [x for x in self.effects if x.ability.func_names & powers_set]

