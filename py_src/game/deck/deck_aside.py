from core import *
from game.card.face import *
from game.deck import *
from game.effect import *

# Only use for scenario deck,
# For `player.additional_deck`, use `ShuffleAllTo`
class SetAsideDeck:

    def __init__(self) -> None:
        self.initialize = False
        self.deck: Deck
        self.discard_pile: Deck
        self.has_discard_pile = False

    def Create(self, by_effect: 'Effect', villain: 'Villain', card_ids: List[str]=[], *, type:DeckType=DeckType.EncounterDeck, has_discard_pile: bool=True) -> None:
        from game.card.face.card_face import CardFace
        from game.card.factory import CardFactory
        from game.message import Message

        if self.initialize:
            return
        self.initialize = True

        self.deck = Deck2(villain.card, type, CardFace)
        by_effect.world.additional_decks.append(self.deck)

        if has_discard_pile:
            self.discard_pile = Deck2(villain.card, DeckType.EncounterDiscardPile, CardFace)
            self.has_discard_pile = has_discard_pile
            self.deck.BindDiscardPile(self.discard_pile, lambda deck, effect: None)
            by_effect.world.additional_discard_piles.append(self.discard_pile)

        CardFactory.GenerateCards(card_ids, self.deck, by_effect.world)
        Message.WhenDeckCreated_Text(self.deck)
        if not self.deck.flags.is_face_up:
            self.deck.Shuffle(by_effect)

    def PushCards(self, faces: List['TC'], by_effect: 'Effect') -> None:
        from game.operate.faces import Faces
        Faces.MoveAllTo(faces, self.deck, by_effect)
        if self.has_discard_pile:
            for face in faces:
                face.card.bind_discard_pile = self.discard_pile

    # def RevealTheTopCard(self, effect: 'Effect'):
    #     if self.deck.GetSize() > 0:
    #         face = self.deck.Get(True)[0]
    #         face.Reveal(None, effect)

    def GetTopCard(self) -> 'CardFace|None':
        return self.deck.GetTop()

    def GetTopCards(self, size: int) -> List['CardFace']:
        return self.deck.GetTops(size)

