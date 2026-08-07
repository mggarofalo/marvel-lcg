from core import *
from game.card.face import *
from game.deck import *
from game.ability import *
from game.message import *
from game.player import *
from cards.paper import Paper
from game.element.resources import Resources

@final
class Resource(ClassCard, FinalType):
    @override
    def __init__(self, paper: 'Paper') -> None:
        super().__init__(paper)

    @override
    def Play(self, player: 'Player', by_effect: 'Effect', message: 'Message2', resource: 'Resources', from_area: 'Deck', is_like_from_hand: bool):
        assert False, f"Resource cards can not be played, {self=}"

