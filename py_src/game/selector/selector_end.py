from typing import Final
from core import *
from game.card.face import *
from game.effect import *
from game.deck import *

class SelectorEnd:
    def __init__(self,
                # Process rule
                peek: bool=False,
                not_move: bool=False,
                not_shuffle: bool=False,
                ):
        self.peek       = peek
        self.not_move   = not_move
        self.not_shuffle: Final = not_shuffle

    def Process(self, effect: 'Effect', targets: Sequence['CardFace']) -> bool:
        # Shuffle
        do_move = True
        if self.not_move:
            do_move = False
        if not self.not_shuffle:
            SelectorEnd.DoShuffle(effect, targets, do_move, False)
        elif do_move:
            SelectorEnd.DoMove(effect, targets, do_move)
        return True

    def OnSelectTargetFailure(self, effect: 'Effect', peeked_faces: Sequence['CardFace']) -> None:
        if self.peek and peeked_faces:
            SelectorEnd.DoShuffle(effect, peeked_faces, False, True)

    @staticmethod
    def DoMove(effect: 'Effect', faces: Sequence['CardFace'], need_move: bool) -> List['Deck']:
        from game.operate.faces import Faces

        places: List[Deck] = []
        moved_faces: List['CardFace'] = []

        for face in faces:
            deck = face.card.area
            if deck.flags.is_deck:
                if not face.card.IsFaceUp():
                    moved_faces.append(face)
                    places.append(face.card.area)

        if need_move:
            if moved_faces:
                Faces.MoveAllToProcessingArea(moved_faces, effect)
        else:
            # if moved_faces:
            #     CardFace.PeekCards(moved_faces, effect)
            pass

        return places

    @staticmethod
    def DoShuffle(effect: 'Effect', faces: Sequence['CardFace'], need_move: bool, is_failure: bool) -> None:
        from game.deck.deck import Deck

        places: List[Deck] = SelectorEnd.DoMove(effect, faces, need_move)

        if need_move or is_failure:
            for deck in Types.RemoveDuplicates(places):
                # A search that took every card leaves its source deck empty,
                # and shuffling nothing is the right answer -- `Deck.Shuffle`
                # already returns without doing anything when `self.cards` is
                # empty, so there was never a case to branch on.
                #
                # What stood here was an `assert deck.GetSize() != 0` directly
                # above a branch written to handle exactly that, whose two
                # lines of intended handling were commented out. Both of those
                # lines are answered by code that already runs (MARVEL-131):
                #
                #   * `AfterDeckRunOut` is **already sent**. `Card.MoveToArea`
                #     checks `from_area.GetSize() == 0` after every move and
                #     sends it there, so `MoveAllToProcessingArea` above has
                #     fired it by the time we get here. Sending it again would
                #     be one rules event raised twice from two sites -- the
                #     MARVEL-122/124 shape, where neither site is singly
                #     mutation-observable.
                #   * `ShuffleWithDiscardPile` would be **wrong here**. The
                #     searched cards are sitting in the processing area and are
                #     on their way back; folding the discard pile in now would
                #     rebuild the deck in the middle of a search, before
                #     anything knows whether it has genuinely run out.
                #
                # The assert was the only thing turning this into a failure,
                # and on a release build `Log.OnCrash` swallowed it: a
                # single-card setup search left its card stranded in the
                # processing area and the game carried on around it.
                deck.Shuffle(effect)

