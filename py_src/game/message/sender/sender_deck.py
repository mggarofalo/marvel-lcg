from . import *
from typing import Final

class SenderDeck:

    ################################################################################
    # Deck
    ################################################################################
    class WhenDeckCreated_Text(TextMessage):
        def __init__(self, deck: 'Deck') -> None:
            super().__init__(world=deck.world)
            text = TransText("{deck} was created", deck=deck)
            self.Present(text, "set" if deck.GetSize() else "")

    class AfterCardMovedToDeck_Text(TextMessage):
        def __init__(self, deck: 'Deck', faces: List['CardFace'], pos: str, effect: 'Effect') -> None:
            super().__init__(world=deck.world)
            # if not deck.flags.is_dealt_encounter:
            text = TransText("{faces} were moved to {deck} {pos} ({effect})", faces=faces, deck=deck, pos=pos, effect=effect.this)
            self.Present(text, "set", *faces)

    class CardsShuffleToDeck_Text(TextMessage):
        def __init__(self, faces: Sequence['CardFace'], deck: 'Deck') -> None:
            super().__init__(world=deck.world)
            text = TransText("{faces} are being shuffled into {deck}", deck=deck, faces=faces)
            self.Present(text, "")

    # TODO: Pre message
    class AfterDeckShuffle(DeckMessage):
        def __init__(self, deck: 'Deck', discard_pile: 'Deck|None') -> None:
            self.discards: Final = discard_pile
            super().__init__(deck=deck)
            if discard_pile:
                text = TransText("{deck} was shuffled with {discard_pile}", deck=deck, discard_pile=discard_pile)
            else:
                text = TransText("{deck} was shuffled", deck=deck)
            self.Present(text, "shuffle")

    # TODO: Pre message
    class AfterDeckRunOut(DeckMessage):
        def __init__(self, deck: 'Deck') -> None:
            super().__init__(deck=deck)
            if deck.flags.is_deck and not deck.flags.is_discards:
                text = TransText("{deck} has run out", deck=deck)
                self.Present(text, "")

    # TODO: Pre message
    class AfterDeckReset(DeckMessage):
        def __init__(self, deck: 'Deck') -> None:
            super().__init__(deck=deck)
            if deck.flags.is_deck and not deck.flags.is_discards:
                text = TransText("{deck} has been reset", deck=deck)
                self.Present(text, "")

