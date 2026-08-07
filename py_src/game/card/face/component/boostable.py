from . import *

# Store boost cards
class Boostable(Component):
    @override
    def __init__(self, card: 'Card') -> None:
        super().__init__(card)
        self.deck = Deck2(self.parent, DeckType.BoostCardsDeck, CardFace)
        # self.deck.bind_card = self.parent

    @override
    def OnDiscard(self, by_effect: 'Effect') -> List['CardFace']:
        return self.deck.GetAll() + super().OnDiscard(by_effect)

    def GiveBoostCard(self, face: 'CardFace', by_effect: 'Effect') -> bool:
        from game.operate.faces import Faces
        return Faces.MoveAllTo([face], self.deck, by_effect) != []

    ################################################################################
    #
    def GetDeck(self) -> 'Deck2[CardFace]':
        return self.deck

