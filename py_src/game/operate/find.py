from . import *

"""
FIND
When instructed to find a card, a player searches each game area where that card could be found (play area, set-aside area, player deck, discard pile, encounter deck, etc.).
• Players should not unnecessarily search game areas if they know where the card they are looking for can be found.
• All game areas are subject to search when resolving a “find” instruction, with the following exceptions:
    » Facedown encounter cards.
    » The victory display.
    » Cards that have been removed from the game.
• If a player is instructed to “find and reveal” a minion that is already in play, that player engages that minion and resolves any keywords and/or triggered abilities that resolve as a result of that minion being revealed (such as that minion’s “When Revealed” ability).
    » That minion retains all attachments and tokens on it.
    » That minion is not considered to be entering play.
    » That minion is considered to engage that player unless it was already engaged with that player.
"""

class Find:

    @staticmethod
    def FindAndPutIntoPlay(by_effect: 'Effect',
                            to_player: 'Player',
                            *,
                            finder: 'CardFinder|None'=None,
                            name: str|None=None,
                            trait: "CardFace.TRAITS|None"=None,
                            card_type: Type['TC']|CardFace=CardFace,
                            if_not_enter_play: Callable[[], Any]|None=None, 
                            ) -> TC|None:
        face = Find.Find(
            by_effect,
            finder=finder,
            name=name,
            trait=trait,
            card_type=card_type,
        )
        not_enter = True
        if face:
            not_enter = False

            if face.IsInPlay():
                not_enter = True
            elif face.PutIntoPlay(to_player, by_effect):
                return face
            else:
                not_enter = True

        if not_enter and if_not_enter_play:
            if_not_enter_play()
        return face

    @staticmethod
    def FindAndReveal(by_effect: 'Effect',
                    to_player: 'Player',
                    *,
                    finder: 'CardFinder|None'=None,
                    name: str|None=None,
                    trait: "CardFace.TRAITS|None"=None,
                    card_type: Type['TC']|CardFace=CardFace,
                    if_no_entered_play: Callable[[], Any]|None=None,
                    ) -> TC|None:
        face = Find.Find(
            by_effect,
            finder=finder,
            name=name,
            trait=trait,
            card_type=card_type
        )
        if face:
            if face.Reveal(to_player, by_effect, if_no_entered_play=if_no_entered_play):
                return face
        return None

    @staticmethod
    def FindAndAttachTo(by_effect: 'Effect',
                        to_face: 'CardFace',
                        *,
                        who_perform: 'Player',
                        finder: 'CardFinder|None'=None,
                        name: str|None=None,
                        trait: "CardFace.TRAITS|None"=None,
                        card_type: Type['TC']|CardFace=CardFace,
                        ) -> TC|None:
        face = Find.Find(
            by_effect,
            who_perform=who_perform,
            finder=finder,
            name=name,
            trait=trait,
            card_type=card_type
        )
        if face and CanAttach.IsType(face):
            if face.AttachTo2(to_face, by_effect):
                return face # type: ignore
        return None

    @staticmethod
    def Find(by_effect: 'Effect',
            *,
            who_perform: 'Player|None'=None,

            finder: 'CardFinder|None'=None,
            name: str|None=None,
            trait: "CardFace.TRAITS|None"=None,
            card_type: Type['TC']|CardFace=CardFace,

            # include_set_aside: bool=True,
            # include_in_play: bool=True,
            # include_player_deck: bool=True,
            # include_player_discard_pile: bool=False,
            # include_player_hand_cards: bool=False,
            # include_player_area: bool=True,

            **kwargs: Unpack['CardFinder.KWArgs'],
            ) -> TC|None:
        from game.operate.search import Search

        if who_perform == None:
            who_perform = by_effect.world.GetFirstPlayer()

        face = Search.SearchForCard(
            by_effect,
            who_perform,
            include_encounter_deck=True,
            include_encounter_discard_pile=True,
            include_set_aside=True,
            include_removed_area=False,
            include_in_play=True,
            include_player_deck="All",
            include_player_discard_pile="All",
            include_player_hand_cards="All",
            include_player_area="All",
            include_victory_display=False,

            # TODO: face up cards in encounter deck
            # faces = faces,

            # not_move=True,

            finder=CardFinder(
                name=name,
                trait=trait,
                card_type=card_type,
                **kwargs) & finder,
        )
        return face # type: ignore

