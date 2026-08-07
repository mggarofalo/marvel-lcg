from typing import Final
from core import *
from game.effect import *
from game.selector import *
from game.card.face import *
from game.selector.selector_range import SelectorRange

CATEGORY_NAME = "SELECTOR"

class SelectorWhere:
    
    WHERE = Literal[
        "EncounterDeckTop",
        "PlayersDeckTop",
        "YourDiscardPileTop",
        "YourDiscardPileTopmost", # 49002
        "YourDiscardPileBottommost", # 18008
        "YourPlayerDeckTop",
        "AnyDecksTopCard",

        # Deck
        "PlayersDiscardPile",
        "VictoryDisplay",

        "YourPlayerDeck",
        "YourDiscardPile",
        "YourHandCards",
        "AnyHandCards",
        "YouControlCards",
        "YourPlayArea",

        "EncounterDiscardPile",

        "InPlay",
        "SetAside",
        "ThisPlaceCards",

        "Default", # program only
    ]

    def __init__(self,
                from_where: List['SELECT.WHERE'],
                ) -> None:
        self.is_not_set: Final = from_where == ["Default"]

        from_where2: List[SelectorWhere.WHERE]
        if self.is_not_set:
            from_where2 = ["InPlay"]
        else:
            from_where2 = [x for x in from_where if x != "Default"]

        self.from_where: Final = from_where2

    def Get(self, effect: 'Effect', selector_range: 'SelectorRange') -> List['CardFace']:
        from game.operate.worlds import Worlds
        from game.selector import Select
        from game.deck import Deck

        found_faces: List['CardFace'] = []
        checked_deck: List['Deck'] = []

        def get_deck_cards(deck: 'Deck', from_top: bool=True) -> List['CardFace']:
            # Fix "21095"
            if deck not in checked_deck:
                checked_deck.append(deck)
            else:
                return []

            if deck.GetSize() == 0:
                return []

            faces = deck.GetAll(from_top)
            if selector_range.select_all:
                return faces

            if where == "YourDiscardPileBottommost":
                return faces
            elif where == "YourDiscardPileTopmost":
                return faces

            if selector_range.can_be_zero:
                size = 1
            else:
                size = selector_range.GetTargetMin(effect, [])

            faces: List['CardFace'] = []
            for face in deck.GetAll(from_top):
                # Fix "45030a" play "27043" from the top
                if face != effect.this:
                    faces.append(face)
                    size -= 1
                    if size <= 0:
                        break
            return faces
        ################################################################################
        #
        def get_EncounterDeckTop():
            faces: List['CardFace'] = []
            for deck in Worlds.GetEncounterDecks(effect):
                faces += get_deck_cards(deck)
            return faces
        def get_PlayersDeckTop():
            faces: List['CardFace'] = []
            for player in Worlds.GetPlayers(effect):
                deck = player.player_deck
                faces += get_deck_cards(deck)
            return faces
        def get_YourDiscardPileTop():
            player = Select.GetYou(effect)
            return get_deck_cards(player.discard_pile)
        def get_YourDiscardPileTopmost():
            player = Select.GetYou(effect)
            return get_deck_cards(player.discard_pile)
        def get_YourDiscardPileBottommost():
            player = Select.GetYou(effect)
            return get_deck_cards(player.discard_pile, from_top=False)
        def get_YourPlayerDeckTop():
            player = Select.GetYou(effect)
            return get_deck_cards(player.player_deck)
        def get_PlayersDiscardPile():
            faces: List['CardFace'] = []
            for player in Worlds.GetPlayers(effect):
                faces += player.discard_pile.Get(True)
            return faces
        def get_AnyDecksTopCard():
            decks: List[Deck] = []
            for player in Worlds.GetPlayers(effect):
                deck = player.player_deck
                if deck.GetSize() > 0:
                    decks.append(deck)
                deck = player.additional_deck
                if deck.GetSize() > 0:
                    decks.append(deck)
            for deck in Worlds.GetEncounterDecks(effect):
                if deck.GetSize() > 0:
                    decks.append(deck)
            faces: List['CardFace'] = []
            for deck in decks:
                face = deck.GetTop()
                if face:
                    faces.append(face)
            return faces
        def get_VictoryDisplay():
            return effect.world.victory_display.Get()
        def get_YourPlayerDeck():
            player = Select.GetYou(effect)
            return player.player_deck.Get(True)
        def get_YourDiscardPile():
            player = Select.GetYou(effect)
            return player.discard_pile.Get(True)
        def get_EncounterDiscardPile():
            return Worlds.GetEncounterDiscardPileCards(effect)
        def get_InPlay():
            faces = Worlds.GetOnFieldCards(effect)
            # OnFieldStatusCard
            # for unit in Worlds.GetOnFieldCharacters(effect):
            #     faces += unit.components.status.GetDeck().Get()
            return faces
        def get_SetAside():
            return Worlds.GetSetAsideAreaCards(effect)
        def get_YouControlCards():
            # In play only
            """
            If a game step or card ability references a card that “you control” or a “player controls,” that game step or card ability only refers to cards in play currently under that player’s control.
            """
            player = Select.GetYou(effect)
            return [player.GetIdentity()] + player.GetControlCardsByType(upgrade=True, support=True, ally=True)
        def get_YourPlayArea():
            player = Select.GetYou(effect)
            return [player.GetIdentity()] + player.GetControlCardsByType(upgrade=True, support=True, ally=True, attachment=True) + player.dealt_encounter_cards.GetAll() + player.engaged_minions.GetAll()
        def get_YourHandCards():
            player = Select.GetYou(effect)
            return player.hand_cards.Get()
        def get_AnyHandCards():
            faces: List['CardFace'] = []
            for player in effect.world.const_players:
                faces += player.hand_cards.Get()
            return faces
        def get_ThisPlaceCards():
            return effect.this.GetPlacedCardArea().Get()

        where_map: Dict['SELECT.WHERE', Callable[[], List['CardFace']]] = {
            "EncounterDeckTop"          : get_EncounterDeckTop,
            "PlayersDeckTop"            : get_PlayersDeckTop,
            "YourDiscardPileTop"        : get_YourDiscardPileTop,
            "YourDiscardPileTopmost"    : get_YourDiscardPileTopmost,
            "YourDiscardPileBottommost" : get_YourDiscardPileBottommost,
            "YourPlayerDeckTop"         : get_YourPlayerDeckTop,
            "PlayersDiscardPile"        : get_PlayersDiscardPile,
            "YourPlayArea"              : get_YourPlayArea,
            "AnyDecksTopCard"           : get_AnyDecksTopCard,
            "VictoryDisplay"            : get_VictoryDisplay,
            "YourPlayerDeck"            : get_YourPlayerDeck,
            "YourDiscardPile"           : get_YourDiscardPile,
            "EncounterDiscardPile"      : get_EncounterDiscardPile,
            "InPlay"                    : get_InPlay,
            "Default"                   : get_InPlay,
            "SetAside"                  : get_SetAside,
            "YouControlCards"           : get_YouControlCards,
            "YourHandCards"             : get_YourHandCards,
            "AnyHandCards"              : get_AnyHandCards,
            "ThisPlaceCards"            : get_ThisPlaceCards,
        }
        for where in self.from_where:
            found_faces += where_map[where]()

        return found_faces

