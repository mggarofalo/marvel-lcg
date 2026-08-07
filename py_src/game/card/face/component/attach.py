from . import *

# We can place a card under this card
class PlacedCard(Component):
    @override
    def __init__(self, card: 'Card') -> None:
        super().__init__(card)
        self.deck = Deck2(self.parent, DeckType.PlaceCardArea, CardFace)
        # self.deck.bind_card = self.parent

    @override
    def OnParentLeavePlay(self, by_effect: 'Effect'):
        from game.effect.rule import GameRule
        from game.operate.faces import Faces

        super().OnParentLeavePlay(by_effect)
        Faces.DiscardAll(self.GetDeck().Get(), GameRule(self.parent.face))

    @override
    def OnDiscard(self, by_effect: 'Effect') -> List['CardFace']:
        return self.deck.GetAll() + super().OnDiscard(by_effect)
        # # See: "11019"
        # self.deck.DiscardAll(by_effect)

    ################################################################################
    #
    def GetDeck(self) -> 'Deck2[CardFace]':
        return self.deck

